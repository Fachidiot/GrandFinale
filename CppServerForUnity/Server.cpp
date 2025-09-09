#include "stdafx.h"
#include "Server.h"

Server::Server(asio::io_context &io_context, short port)
    : io_context_(io_context), acceptor_(io_context, tcp::endpoint(tcp::v4(), port))
{
    initialize_request_handlers();
    std::cout << "Server started on port " << port << std::endl;
}

Server::~Server()
{
    for (auto &t : session_threads_)
    {
        if (t.joinable())
        {
            t.join();
        }
    }
}

void Server::run()
{
    start_accept();
    io_context_.run();
}

void Server::initialize_request_handlers()
{
    request_handlers_["create_room"] = [this](const json &req, auto socket)
    { handle_create_room(req, socket); };
    request_handlers_["find_rooms"] = [this](const json &req, auto socket)
    { handle_find_rooms(req, socket); };
    request_handlers_["join_room"] = [this](const json &req, auto socket)
    { handle_join_room(req, socket); };
    request_handlers_["chat_message"] = [this](const json &req, auto socket)
    { handle_chat_message(req, socket); };
    request_handlers_["leave_room"] = [this](const json &req, auto socket)
    { handle_leave_room(req, socket); };
    request_handlers_["toggle_ready"] = [this](const json &req, auto socket)
    { handle_toggle_ready(req, socket); };
    request_handlers_["start_game"] = [this](const json &req, auto socket)
    { handle_start_game(req, socket); };
    request_handlers_["set_nickname"] = [this](const json &req, auto socket)
    { handle_set_nickname(req, socket); };
    request_handlers_["player_input"] = [this](const json &req, auto socket)
    { handle_player_input(req, socket); };
}

void Server::start_accept()
{
    auto socket = std::make_shared<tcp::socket>(io_context_);
    acceptor_.async_accept(*socket, [this, socket](const asio::error_code &error)
                           { handle_accept(socket, error); });
}

void Server::handle_accept(std::shared_ptr<tcp::socket> socket, const asio::error_code &error)
{
    if (!error)
    {
        session_threads_.emplace_back([this, socket]()
                                      { session(socket); });
    }
    start_accept();
}

void Server::session(std::shared_ptr<tcp::socket> socket)
{
    try
    {
        { // 임의의 id값 부여해서 player에게 넘겨주기.
            std::lock_guard<std::mutex> lock(mutex_);
            std::string player_id = "Player" + std::to_string(next_player_id_num_++);
            connected_players_[socket] = {player_id, "", -1, false};
            std::cout << player_id << " connected." << std::endl;

            json id_message;
            id_message["type"] = "assign_id";
            id_message["player_id"] = player_id;
            safe_write(socket, id_message.dump());
        }

        for (;;)
        {
            char data[1024];
            asio::error_code error;
            size_t length = socket->read_some(asio::buffer(data), error);

            if (error == asio::error::eof)
            {
                handle_disconnect(socket);
                break;
            }
            if (error)
            {
                throw asio::system_error(error);
            }

            std::string message(data, length);
            auto request_json = json::parse(message);
            handle_request(request_json, socket);
        }
    }
    catch (std::exception &e)
    {
        std::cerr << "Exception in session: " << e.what() << std::endl;
        handle_disconnect(socket);
    }
}

void Server::handle_request(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::string type = request.value("type", "");
    auto it = request_handlers_.find(type);
    if (it != request_handlers_.end())
    {
        it->second(request, socket);
    }
    else
    {
        std::cerr << "Unknown request type: " << type << std::endl;
    }
}

void Server::safe_write(const std::shared_ptr<tcp::socket> &socket, const std::string &msg)
{
    try
    {
        asio::write(*socket, asio::buffer(msg + "\n"));
    }
    catch (std::exception &e)
    {
        std::cerr << "Write failed for player " << connected_players_[socket].id << ": " << e.what() << std::endl;
        handle_disconnect(socket);
    }
}

void Server::broadcast_room_update(int room_id)
{
    if (active_rooms_.count(room_id) == 0)
    {
        std::cout << "Warning! no room in active_rooms" << std::endl;
        return;
    }

    Room &room = active_rooms_[room_id];
    json room_update;
    room_update["type"] = "update_room_info";
    room_update["room_name"] = room.name;

    std::cout << "Broadcasting a room " << room.name << std::endl;

    if (room.host_socket)
    {
        room_update["host_id"] = connected_players_[room.host_socket].id;
    }
    else
    {
        room_update["host_id"] = "";
    }

    json players_array = json::array();
    for (const auto &player_socket : room.players)
    {
        json player_info;
        player_info["player_id"] = connected_players_[player_socket].id;
        player_info["nickname"] = connected_players_[player_socket].nickname;
        player_info["is_ready"] = connected_players_[player_socket].is_ready;

        json pos_json;
        pos_json["x"] = connected_players_[player_socket].position.x;
        pos_json["y"] = connected_players_[player_socket].position.y;
        pos_json["z"] = connected_players_[player_socket].position.z;
        player_info["position"] = pos_json;
        players_array.push_back(player_info);
    }
    room_update["players"] = players_array;

    std::string update_str = room_update.dump();
    for (const auto &player_socket : room.players)
    {
        safe_write(player_socket, update_str);
    }
}

void Server::handle_disconnect(std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);

    if (connected_players_.find(socket) == connected_players_.end())
        return;

    int current_room_id = connected_players_[socket].room_id;
    std::string leaving_player_id = connected_players_[socket].id;

    std::cout << leaving_player_id << " disconnected." << std::endl;

    if (current_room_id != -1 && active_rooms_.count(current_room_id))
    {
        // Notify others that a player has left
        json left_msg;
        left_msg["type"] = "player_left";
        left_msg["player_id"] = leaving_player_id;
        std::string left_str = left_msg.dump();
        for (const auto &player_socket : active_rooms_[current_room_id].players)
        {
            if (player_socket != socket)
            {
                safe_write(player_socket, left_str);
            }
        }

        auto &players_in_room = active_rooms_[current_room_id].players;
        players_in_room.erase(std::remove(players_in_room.begin(), players_in_room.end(), socket), players_in_room.end());

        if (players_in_room.empty())
        {
            active_rooms_.erase(current_room_id);
        }
        else
        {
            if (active_rooms_[current_room_id].host_socket == socket)
            {
                active_rooms_[current_room_id].host_socket = players_in_room.front();
            }
            broadcast_room_update(current_room_id);
        }
    }
    connected_players_.erase(socket);
}

// --- Request Handler Implementations ---

void Server::handle_set_nickname(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    std::string nickname = request["nickname"];
    connected_players_[socket].nickname = nickname;
    std::cout << "Player " << connected_players_[socket].id << " set nickname to " << nickname << std::endl;
}

void Server::handle_create_room(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    int room_id = next_room_id_++;
    std::string room_name = request["room_name"];
    std::cout << "Creating a new room : " << room_name << std::endl;

    Room new_room;
    new_room.id = room_id;
    new_room.name = room_name;
    new_room.players.push_back(socket);
    new_room.host_socket = socket;
    active_rooms_[room_id] = new_room;

    connected_players_[socket].room_id = room_id;

    broadcast_room_update(room_id);
}

void Server::handle_find_rooms(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    std::cout << "Finding rooms..." << std::endl;
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
    safe_write(socket, response.dump());
}

void Server::handle_join_room(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    std::cout << "Joining a room..." << std::endl;
    int room_id_to_join = request["room_id"];
    if (active_rooms_.count(room_id_to_join))
    {
        // Notify others that a player has joined
        json joined_msg;
        joined_msg["type"] = "player_joined";
        joined_msg["player_id"] = connected_players_[socket].id;
        std::string joined_str = joined_msg.dump();
        for (const auto &player_socket : active_rooms_[room_id_to_join].players)
        {
            safe_write(player_socket, joined_str);
        }

        // Add player to room
        active_rooms_[room_id_to_join].players.push_back(socket);
        connected_players_[socket].room_id = room_id_to_join;

        // Set initial random position on the server
        float x = static_cast<float>(rand()) / (static_cast<float>(RAND_MAX / 10.0f)) - 5.0f;
        float z = static_cast<float>(rand()) / (static_cast<float>(RAND_MAX / 10.0f)) - 5.0f;
        connected_players_[socket].position = {x, 0, z};

        broadcast_room_update(room_id_to_join);
    }
}

void Server::handle_chat_message(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    int current_room_id = connected_players_[socket].room_id;
    if (current_room_id != -1)
    {
        json broadcast_msg;
        broadcast_msg["type"] = "chat_broadcast";
        broadcast_msg["sender_id"] = connected_players_[socket].nickname;
        broadcast_msg["message"] = request["message"];
        std::string broadcast_str = broadcast_msg.dump();

        for (auto &player_socket : active_rooms_[current_room_id].players)
        {
            safe_write(player_socket, broadcast_str);
        }
    }
}

void Server::handle_leave_room(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    int current_room_id = connected_players_[socket].room_id;
    if (current_room_id != -1)
    {
        std::cout << connected_players_[socket].id << " is leaving room " << current_room_id << std::endl;

        // Notify others that a player has left
        json left_msg;
        left_msg["type"] = "player_left";
        left_msg["player_id"] = connected_players_[socket].id;
        std::string left_str = left_msg.dump();
        for (const auto &player_socket : active_rooms_[current_room_id].players)
        {
            if (player_socket != socket) // Don't send to the player who is leaving
            {
                safe_write(player_socket, left_str);
            }
        }

        auto &players_in_room = active_rooms_[current_room_id].players;
        players_in_room.erase(std::remove(players_in_room.begin(), players_in_room.end(), socket), players_in_room.end());

        connected_players_[socket].room_id = -1;

        if (players_in_room.empty())
        {
            active_rooms_.erase(current_room_id);
            std::cout << "Room " << current_room_id << " is empty and has been removed" << std::endl;
        }
        else
        {
            if (active_rooms_[current_room_id].host_socket == socket)
            {
                active_rooms_[current_room_id].host_socket = players_in_room.front();
            }
            broadcast_room_update(current_room_id);
        }

        json response;
        response["type"] = "leave_room_success";
        safe_write(socket, response.dump());
    }
}

void Server::handle_toggle_ready(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    auto &player = connected_players_[socket];
    int current_room_id = player.room_id;
    if (current_room_id != -1 && active_rooms_[current_room_id].host_socket != socket)
    {
        player.is_ready = !player.is_ready;
        broadcast_room_update(current_room_id);
    }
}

void Server::handle_start_game(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    int current_room_id = connected_players_[socket].room_id;
    if (current_room_id != -1 && active_rooms_[current_room_id].host_socket == socket)
    {
        bool all_ready = true;
        Room &room = active_rooms_[current_room_id];
        for (const auto &player_socket : room.players)
        {
            if (player_socket != room.host_socket && !connected_players_[player_socket].is_ready)
            {
                all_ready = false;
                break;
            }
        }

        if (all_ready && room.players.size() > 1)
        {
            json game_start_msg;
            game_start_msg["type"] = "game_start";
            std::string msg_start = game_start_msg.dump();
            for (const auto &player_socket : room.players)
            {
                safe_write(player_socket, msg_start);
            }
        }
        else
        {
            // Optionally, send a failure message to the host
        }
    }
}

void Server::handle_player_input(const json &request, std::shared_ptr<tcp::socket> socket)
{
    std::lock_guard<std::mutex> lock(mutex_);
    int current_room_id = connected_players_[socket].room_id;
    if (current_room_id != -1)
    {
        auto &player = connected_players_[socket];

        // Get input from request
        float h = request["input"]["h"].get<float>();
        float v = request["input"]["v"].get<float>();

        // Server-side movement calculation
        float speed = 5.0f;
        float deltaTime = 0.1f; // Assuming fixed tick rate corresponding to client send rate
        vec3 direction = {h, 0, v};
        // Normalize direction if it's not zero
        float length = std::sqrt(direction.x * direction.x + direction.z * direction.z);
        if (length > 0.01f)
        {
            direction.x /= length;
            direction.z /= length;
        }

        player.position.x += direction.x * speed * deltaTime;
        player.position.z += direction.z * speed * deltaTime;

        // Broadcast the new position to everyone in the room
        json broadcast_msg;
        broadcast_msg["type"] = "player_moved";
        broadcast_msg["player_id"] = player.id;
        json pos_json;
        pos_json["x"] = player.position.x;
        pos_json["y"] = player.position.y;
        pos_json["z"] = player.position.z;
        broadcast_msg["position"] = pos_json;
        std::string broadcast_str = broadcast_msg.dump();

        for (auto &player_socket : active_rooms_[current_room_id].players)
        {
            safe_write(player_socket, broadcast_str);
        }
    }
}