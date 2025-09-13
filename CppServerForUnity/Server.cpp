
#include "Server.h"
#include "Session.h"

Server::Server(asio::io_context &io_context, short port)
    : io_context_(io_context),
      acceptor_(io_context, tcp::endpoint(tcp::v4(), port)),
      server_strand_(io_context.get_executor()),
      game_loop_timer_(io_context)
{
    initialize_request_handlers();
    std::cout << "Server started on port " << port << std::endl;
}

void Server::run()
{
    start_accept();
    start_game_loop();

    // Create a thread pool to run the io_context
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
        if (!error)
        {
            std::make_shared<Session>(std::move(socket), *this)->start();
        }
        start_accept(); });
}

void Server::handle_connect(std::shared_ptr<Session> session)
{
    asio::post(server_strand_, [this, session]()
               {
        std::string player_id = "UID" + std::to_string(next_player_id_num_++);
        connected_players_[session] = { player_id, "", -1, false };
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
        if (connected_players_.find(session) == connected_players_.end())
            return;

        int current_room_id = connected_players_[session].room_id;
        std::string leaving_player_id = connected_players_[session].id;

        std::cout << leaving_player_id << " disconnected." << std::endl;

        if (current_room_id != -1 && active_rooms_.count(current_room_id))
        {
            auto& room = active_rooms_[current_room_id];
            room.players.erase(std::remove(room.players.begin(), room.players.end(), session), room.players.end());

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
            // Post the handler to the server's main strand to ensure all state changes are synchronized
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

// --- Request Handler Implementations ---
// All handlers are now executed within the server_strand_, so no explicit locking is needed.

void Server::initialize_request_handlers()
{
    request_handlers_["create_room"] = [this](auto session, const json &req)
    { handle_create_room(session, req); };
    request_handlers_["find_rooms"] = [this](auto session, const json &req)
    { handle_find_rooms(session, req); };
    request_handlers_["join_room"] = [this](auto session, const json &req)
    { handle_join_room(session, req); };
    request_handlers_["chat_message"] = [this](auto session, const json &req)
    { handle_chat_message(session, req); };
    request_handlers_["leave_room"] = [this](auto session, const json &req)
    { handle_leave_room(session, req); };
    request_handlers_["toggle_ready"] = [this](auto session, const json &req)
    { handle_toggle_ready(session, req); };
    request_handlers_["start_game"] = [this](auto session, const json &req)
    { handle_start_game(session, req); };
    request_handlers_["set_nickname"] = [this](auto session, const json &req)
    { handle_set_nickname(session, req); };
    request_handlers_["player_input"] = [this](auto session, const json &req)
    { handle_player_input(session, req); };
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
        // Position data will be sent via game state updates, not here.
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

        // Set initial random position
        float x = static_cast<float>(rand()) / (static_cast<float>(RAND_MAX / 10.0f)) - 5.0f;
        float z = static_cast<float>(rand()) / (static_cast<float>(RAND_MAX / 10.0f)) - 5.0f;
        connected_players_[session].position = {x, 0, z};

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
    // This logic remains largely the same, but within the strand
    int current_room_id = connected_players_[session].room_id;
    if (current_room_id != -1 && active_rooms_[current_room_id].host == session)
    {
        // ... game start logic ...
    }
}

void Server::handle_player_input(std::shared_ptr<Session> session, const json &request)
{
    // IMPORTANT: This now only STORES the input. The game loop will process it.
    auto &player = connected_players_[session];
    if (player.room_id != -1)
    {
        try
        {
            player.input_h = request.at("input").at("h").get<float>();
            player.input_v = request.at("input").at("v").get<float>();
            player.anim_forward = request.at("input").at("anim_forward").get<float>();
            player.anim_strafe = request.at("input").at("anim_strafe").get<float>();
        }
        catch (json::exception &e)
        {
            std::cerr << "Error parsing player_input: " << e.what() << std::endl;
        }
    }
}

void Server::start_game_loop()
{
    game_loop_timer_.expires_after(tick_interval_);
    game_loop_timer_.async_wait([this](const asio::error_code &ec)
                                {
        if (!ec)
        {
            tick();
        } });
}

void Server::tick()
{
    // Post the game logic to the main server strand to ensure thread safety
    asio::post(server_strand_, [this]()
               {
        float deltaTime = static_cast<float>(tick_interval_.count()) / 1000.0f;
        const float speed = 5.0f;

        for (auto& [room_id, room] : active_rooms_)
        {
            if (room.players.empty()) continue;

            json all_players_state = json::array();

            // First, update all player positions based on their last input
            for (auto& player_session : room.players)
            {
                if (connected_players_.count(player_session) == 0) continue;

                auto& player = connected_players_[player_session];

                // Calculate movement
                vec3 direction = { player.input_h, 0, player.input_v };
                float length = std::sqrt(direction.x * direction.x + direction.z * direction.z);
                if (length > 0.01f)
                {
                    direction.x /= length;
                    direction.z /= length;
                }

                player.position.x += direction.x * speed * deltaTime;
                player.position.z += direction.z * speed * deltaTime;

                // Create JSON object for this player's state
                json player_state;
                player_state["player_id"] = player.id;
                
                json pos_json;
                pos_json["x"] = player.position.x;
                pos_json["y"] = player.position.y;
                pos_json["z"] = player.position.z;
                player_state["position"] = pos_json;

                json anim_json;
                anim_json["forward"] = player.anim_forward;
                anim_json["strafe"] = player.anim_strafe;
                player_state["animation"] = anim_json;

                all_players_state.push_back(player_state);
            }

            // Then, broadcast the complete game state to all players in the room
            json game_state_update;
            game_state_update["type"] = "game_state_update";
            game_state_update["players"] = all_players_state;
            std::string state_str = game_state_update.dump();

            for (auto& player_session : room.players)
            {
                player_session->write(state_str);
            }
        } });

    // Schedule the next tick
    start_game_loop();
}
