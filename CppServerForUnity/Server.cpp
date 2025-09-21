#include "Server.h"
#include "Session.h"

Server::Server(asio::io_context &io_context, short tcp_port, short udp_port)
    : io_context_(io_context),
      acceptor_(io_context, tcp::endpoint(tcp::v4(), tcp_port)),
      udp_socket_(io_context, udp::endpoint(udp::v4(), udp_port)),
      server_strand_(io_context.get_executor()),
      game_loop_timer_(io_context)
{
    initialize_request_handlers();
    std::cout << "Server started on TCP port " << tcp_port << " and UDP port " << udp_port << std::endl;
}

void Server::run()
{
    start_accept();
    start_udp_receive();
    start_game_loop();

    const int thread_count = std::max(1, (int)std::thread::hardware_concurrency());
    thread_pool_.reserve(thread_count);
    for (int i = 0; i < thread_count; ++i)
    {
        thread_pool_.emplace_back([this]()
                                  { io_context_.run(); });
    }

    for (auto &t : thread_pool_)
    {
        if (t.joinable())
        {
            t.join();
        }
    }
}

void Server::start_accept()
{
    acceptor_.async_accept([this](const asio::error_code &error, tcp::socket socket)
                           {
        if (!error) { 
            std::make_shared<Session>(std::move(socket), *this)->start(); 
        }
        start_accept(); });
}

void Server::start_udp_receive()
{
    udp_socket_.async_receive_from(
        asio::buffer(udp_buffer_),
        remote_udp_endpoint_,
        [this](const asio::error_code &error, std::size_t bytes_transferred)
        {
            handle_udp_receive(error, bytes_transferred);
        });
}

void Server::handle_udp_receive(const asio::error_code &error, std::size_t bytes_transferred)
{
    if (!error && bytes_transferred > 0)
    {
        std::string message(udp_buffer_.data(), bytes_transferred);

        asio::post(server_strand_, [this, message, sender_endpoint = this->remote_udp_endpoint_]()
                   {
            try
            {
                auto request_json = json::parse(message);
                std::string type = request_json.value("type", "");

                std::string player_id = request_json.value("player_id", "");
                if (player_id.empty()) return;

                player_udp_endpoints_[player_id] = sender_endpoint;
                Player* player_ptr = nullptr;
                for (auto& pair : connected_players_) {
                    if (pair.second.id == player_id) {
                        player_ptr = &pair.second;
                        break;
                    }
                }
                if (!player_ptr) return;

                if (type == "transform_update")
                {
                    int view_id = request_json["view_id"];
                    const auto &pos = request_json["position"];
                    const auto &rot = request_json["rotation"];

                    if (view_id == 0) { 
                        player_ptr->body_position = {pos["x"], pos["y"], pos["z"]};
                        player_ptr->body_rotation = {rot["x"], rot["y"], rot["z"], rot["w"]};
                    }
                    else if (view_id == 1) { 
                        player_ptr->camera_rotation = {rot["x"], rot["y"], rot["z"], rot["w"]};
                    }
                }
                else if (type == "anim_sync")
                {
                    player_ptr->anim_x = request_json.value("x", 0.0f);
                    player_ptr->anim_y = request_json.value("y", 0.0f);
                    player_ptr->anim_walk = request_json.value("walk", false);
                    player_ptr->anim_sprint = request_json.value("sprint", false);
                    player_ptr->anim_roll = request_json.value("roll", false);
                    player_ptr->anim_isGrounded = request_json.value("isGrounded", true);
                    player_ptr->anim_crouch = request_json.value("crouch", false);
                }
            }
            catch (json::parse_error &e) { std::cerr << "UDP JSON parse error: " << e.what() << std::endl; } });
    }
    start_udp_receive();
}

void Server::tick()
{
    asio::post(server_strand_, [this]()
               {
        for (auto const &[room_id, room] : active_rooms_)
        {
            if (room.players.empty()) continue;

            json all_players_state = json::array();

            for (const auto &player_session : room.players)
            {
                if (connected_players_.count(player_session) == 0) continue;
                const auto &player = connected_players_.at(player_session);

                json player_state;
                player_state["player_id"] = player.id;
                player_state["body_pos"] = {
                    {"x", player.body_position.x}, 
                    {"y", player.body_position.y}, 
                    {"z", player.body_position.z}
                };
                player_state["body_rot"] = {
                    {"x", player.body_rotation.x}, 
                    {"y", player.body_rotation.y}, 
                    {"z", player.body_rotation.z}, 
                    {"w", player.body_rotation.w}
                };
                player_state["cam_rot"] = {
                    {"x", player.camera_rotation.x}, 
                    {"y", player.camera_rotation.y}, 
                    {"z", player.camera_rotation.z}, 
                    {"w", player.camera_rotation.w}
                };
                player_state["x"] = player.anim_x;
                player_state["y"] = player.anim_y;
                player_state["walk"] = player.anim_walk;
                player_state["sprint"] = player.anim_sprint;
                player_state["roll"] = player.anim_roll;
                player_state["isGrounded"] = player.anim_isGrounded;
                player_state["crouch"] = player.anim_crouch;
                player_state["weapon_id"] = player.current_weapon_id; // Include current weapon IDS

                all_players_state.push_back(player_state);
            }

            json game_state_update;
            game_state_update["type"] = "game_state";
            game_state_update["updates"] = all_players_state;
            
            auto state_str_ptr = std::make_shared<std::string>(game_state_update.dump());

            for (const auto &player_session : room.players)
            {
                const auto &player = connected_players_.at(player_session);
                auto it = player_udp_endpoints_.find(player.id);
                if (it != player_udp_endpoints_.end())
                {
                    udp_socket_.async_send_to(asio::buffer(*state_str_ptr), it->second, [state_str_ptr](const asio::error_code &, std::size_t) {});
                }
            }
        } });

    start_game_loop();
}

void Server::handle_player_action(std::shared_ptr<Session> session, const json &request)
{
    if (connected_players_.find(session) == connected_players_.end())
        return;

    auto &player = connected_players_.at(session);
    int current_room_id = player.room_id;
    std::string action = request.value("action", "");

    if (current_room_id != -1)
    {
        json broadcast_msg;
        broadcast_msg["player_id"] = player.id;

        if (action == "shoot")
        {
            broadcast_msg["type"] = "player_event";
            broadcast_msg["event"] = "shoot";
            std::cout << player.nickname << " Do Shoot" << std::endl;
        }
        else if (action == "weapon_change")
        {
            int weapon_id = request.value("weapon_id", 1);
            player.current_weapon_id = weapon_id; // Update server state

            broadcast_msg["type"] = "player_event";
            broadcast_msg["event"] = "weapon_change";
            broadcast_msg["weapon_id"] = weapon_id;
            std::cout << player.nickname << " Do Change Weapon to " << weapon_id << std::endl;
        }

        if (!broadcast_msg.empty())
        {
            std::string broadcast_str = broadcast_msg.dump();
            for (auto &player_session : active_rooms_[current_room_id].players)
            {
                if (player_session != session)
                {
                    player_session->write(broadcast_str);
                }
            }
        }
    }
}

void Server::handle_connect(std::shared_ptr<Session> session)
{ //
    asio::post(server_strand_, [this, session]()
               {
        std::string player_id = "UID" + std::to_string(next_player_id_num_++);
        connected_players_[session] = {player_id, "", -1, false};
        std::cout << player_id << " connected." << std::endl;

        json id_message;
        id_message["type"] = "assign_id";
        id_message["player_id"] = player_id;
        session->write(id_message.dump()); });
}

void Server::handle_disconnect(std::shared_ptr<Session> session)
{
    asio::post(server_strand_, [this, session]()
               {
        if (connected_players_.find(session) == connected_players_.end()) return;

        std::string leaving_player_id = connected_players_[session].id;
        int current_room_id = connected_players_[session].room_id;

        std::cout << leaving_player_id << " disconnected." << std::endl;
        player_udp_endpoints_.erase(leaving_player_id);

        if (current_room_id != -1 && active_rooms_.count(current_room_id))
        {
            auto &room = active_rooms_[current_room_id];
            room.players.erase(std::remove(room.players.begin(), room.players.end(), session), room.players.end());

            if (room.players.empty())
            {
                active_rooms_.erase(current_room_id);
            }
            else
            {
                if (room.host == session) { room.host = room.players.front(); }
                broadcast_room_update(current_room_id);
            }
        }
        connected_players_.erase(session); });
}

void Server::handle_request(std::shared_ptr<Session> session, const std::string &message)
{
    try
    {
        auto request_json = json::parse(message);
        std::string type = request_json.value("type", "");

        auto it = request_handlers_.find(type);
        if (it != request_handlers_.end())
        {
            asio::post(server_strand_, [this, session, request_json, handler = it->second]()
                       { handler(session, request_json); });
        }
        else
        {
            std::cerr << "Unknown request type: " << type << std::endl;
        }
    }
    catch (json::parse_error &e)
    {
        std::cerr << "JSON parse error: " << e.what() << std::endl;
    }
}

void Server::initialize_request_handlers()
{
    request_handlers_["create_room"] = [this](auto s, const auto &r)
    { handle_create_room(s, r); };
    request_handlers_["find_rooms"] = [this](auto s, const auto &r)
    { handle_find_rooms(s, r); };
    request_handlers_["join_room"] = [this](auto s, const auto &r)
    { handle_join_room(s, r); };
    request_handlers_["chat_message"] = [this](auto s, const auto &r)
    { handle_chat_message(s, r); };
    request_handlers_["leave_room"] = [this](auto s, const auto &r)
    { handle_leave_room(s, r); };
    request_handlers_["toggle_ready"] = [this](auto s, const auto &r)
    { handle_toggle_ready(s, r); };
    request_handlers_["start_game"] = [this](auto s, const auto &r)
    { handle_start_game(s, r); };
    request_handlers_["set_nickname"] = [this](auto s, const auto &r)
    { handle_set_nickname(s, r); };
    request_handlers_["player_action"] = [this](auto s, const auto &r)
    { handle_player_action(s, r); };
}

void Server::broadcast_room_update(int room_id)
{
    if (active_rooms_.count(room_id) == 0)
        return;

    Room &room = active_rooms_[room_id];
    json room_update;
    room_update["type"] = "update_room_info";
    room_update["room_name"] = room.name;
    room_update["host_id"] = room.host ? connected_players_[room.host].id : "";

    json players_array = json::array();
    for (const auto &player_session : room.players)
    {
        const auto &player_data = connected_players_[player_session];
        json player_info;
        player_info["player_id"] = player_data.id;
        player_info["nickname"] = player_data.nickname;
        player_info["is_ready"] = player_data.is_ready;
        players_array.push_back(player_info);
    }
    room_update["players"] = players_array;

    std::string update_str = room_update.dump();
    for (const auto &player_session : room.players)
    {
        player_session->write(update_str);
    }
    std::cout << "broadcast_room_update" << std::endl;
}

void Server::handle_set_nickname(std::shared_ptr<Session> session, const json &request)
{
    std::string nickname = request["nickname"];
    connected_players_[session].nickname = nickname;
    std::cout << connected_players_[session].id << "'s nickname set " << nickname << std::endl;

    json nickname_message;
    nickname_message["type"] = "assign_nickname";
    nickname_message["player_nickname"] = nickname;
    session->write(nickname_message.dump());
}

void Server::handle_create_room(std::shared_ptr<Session> session, const json &request)
{
    int room_id = next_room_id_++;
    std::string room_name = request["room_name"];

    Room new_room;
    new_room.id = room_id;
    new_room.name = room_name;
    new_room.players.push_back(session);
    new_room.host = session;
    active_rooms_[room_id] = new_room;

    connected_players_[session].room_id = room_id;
    broadcast_room_update(room_id);
    std::cout << room_name << " Room is create from " << connected_players_[session].id << std::endl;
}

void Server::handle_find_rooms(std::shared_ptr<Session> session, const json &request)
{
    json response;
    response["type"] = "find_rooms_response";
    json rooms_array = json::array();
    for (auto const &[id, room] : active_rooms_)
    {
        json room_info;
        room_info["room_id"] = room.id;
        room_info["room_name"] = room.name;
        room_info["player_count"] = room.players.size();
        rooms_array.push_back(room_info);
    }
    response["rooms"] = rooms_array;
    session->write(response.dump());
    std::cout << "finding room request" << std::endl;
}

void Server::handle_join_room(std::shared_ptr<Session> session, const json &request)
{
    int room_id_to_join = request["room_id"];
    if (active_rooms_.count(room_id_to_join))
    {
        active_rooms_[room_id_to_join].players.push_back(session);
        connected_players_[session].room_id = room_id_to_join;

        float x = static_cast<float>(rand()) / (static_cast<float>(RAND_MAX / 10.0f)) - 5.0f;
        float z = static_cast<float>(rand()) / (static_cast<float>(RAND_MAX / 10.0f)) - 5.0f;
        connected_players_[session].body_position = {x, 0, z};

        broadcast_room_update(room_id_to_join);
    }
    std::cout << connected_players_[session].id << " is join at" << active_rooms_[room_id_to_join].name << " Room" << std::endl;
}

void Server::handle_chat_message(std::shared_ptr<Session> session, const json &request)
{
    int current_room_id = connected_players_[session].room_id;
    if (current_room_id != -1)
    {
        json broadcast_msg;
        broadcast_msg["type"] = "chat_broadcast";
        broadcast_msg["sender_id"] = connected_players_[session].nickname;
        broadcast_msg["message"] = request["message"];
        std::string broadcast_str = broadcast_msg.dump();

        for (auto &player_session : active_rooms_[current_room_id].players)
        {
            player_session->write(broadcast_str);
        }
    }
}

void Server::handle_leave_room(std::shared_ptr<Session> session, const json &request)
{
    int current_room_id = connected_players_[session].room_id;
    if (current_room_id != -1)
    {
        auto &room = active_rooms_[current_room_id];
        room.players.erase(std::remove(room.players.begin(), room.players.end(), session), room.players.end());
        connected_players_[session].room_id = -1;

        if (room.players.empty())
        {
            active_rooms_.erase(current_room_id);
        }
        else
        {
            if (room.host == session)
            {
                room.host = room.players.front();
            }
            broadcast_room_update(current_room_id);
        }

        json response;
        response["type"] = "leave_room_success";
        session->write(response.dump());
    }
    std::cout << connected_players_[session].id << " is leave at" << active_rooms_[current_room_id].name << " Room" << std::endl;
}

void Server::handle_toggle_ready(std::shared_ptr<Session> session, const json &request)
{
    auto &player = connected_players_[session];
    int current_room_id = player.room_id;
    if (current_room_id != -1 && active_rooms_[current_room_id].host != session)
    {
        player.is_ready = !player.is_ready;
        broadcast_room_update(current_room_id);
    }
}

void Server::handle_start_game(std::shared_ptr<Session> session, const json &request)
{
    int current_room_id = connected_players_[session].room_id;
    if (current_room_id != -1 && active_rooms_[current_room_id].host == session)
    {
        // ... game start logic ...
    }
}

void Server::start_game_loop()
{
    game_loop_timer_.expires_after(tick_interval_);
    game_loop_timer_.async_wait([this](const asio::error_code &ec)
                                {
        if (!ec) { tick(); } });
}