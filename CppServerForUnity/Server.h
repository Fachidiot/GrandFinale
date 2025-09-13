#pragma once

#include "stdafx.h"
#include "Player.h"
#include "Room.h"

// Forward declaration of Session class
class Session;

class Server
{
public:
    Server(asio::io_context& io_context, short port);
    void run();

    // Game Loop
    void start_game_loop();
    void tick();

    // Interface for Session class to interact with the server
    void handle_connect(std::shared_ptr<Session> session);
    void handle_disconnect(std::shared_ptr<Session> session);
    void handle_request(std::shared_ptr<Session> session, const std::string& message);

private:
    void start_accept();
    void handle_accept(tcp::socket socket, const asio::error_code& error);

    // Request Handlers
    void initialize_request_handlers();
    void handle_create_room(std::shared_ptr<Session> session, const json& req);
    void handle_find_rooms(std::shared_ptr<Session> session, const json& req);
    void handle_join_room(std::shared_ptr<Session> session, const json& req);
    void handle_chat_message(std::shared_ptr<Session> session, const json& req);
    void handle_leave_room(std::shared_ptr<Session> session, const json& req);
    void handle_toggle_ready(std::shared_ptr<Session> session, const json& req);
    void handle_start_game(std::shared_ptr<Session> session, const json& req);
    void handle_set_nickname(std::shared_ptr<Session> session, const json& req);
    void handle_player_input(std::shared_ptr<Session> session, const json& req);

    // Utility
    void broadcast_room_update(int room_id);

    tcp::acceptor acceptor_;
    asio::io_context& io_context_;
    asio::steady_timer game_loop_timer_;
    const std::chrono::milliseconds tick_interval_{50}; // 20 ticks per second
    
    // Use a single strand for managing shared resources like rooms and players
    // This is a simpler approach than per-room mutexes for now.
    asio::strand<asio::io_context::executor_type> server_strand_;

    std::map<int, Room> active_rooms_;
    std::map<std::shared_ptr<Session>, Player> connected_players_;
    std::atomic<int> next_room_id_{0};
    std::atomic<int> next_player_id_num_{0};

    std::vector<std::thread> thread_pool_;
    std::map<std::string, std::function<void(std::shared_ptr<Session>, const json&)>> request_handlers_;
};

