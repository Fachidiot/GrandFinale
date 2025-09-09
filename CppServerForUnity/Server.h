#pragma once

#include "stdafx.h"
#include "Player.h"
#include "Room.h"

class Server
{
public:
    Server(asio::io_context &io_context, short port);
    ~Server();

    void run();

private:
    void start_accept();
    void handle_accept(std::shared_ptr<tcp::socket> socket, const asio::error_code &error);
    void session(std::shared_ptr<tcp::socket> socket);

    // Request Handlers
    void initialize_request_handlers();
    void handle_request(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_create_room(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_find_rooms(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_join_room(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_chat_message(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_leave_room(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_toggle_ready(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_start_game(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_set_nickname(const json &request, std::shared_ptr<tcp::socket> socket);
    void handle_player_input(const json &request, std::shared_ptr<tcp::socket> socket);

    // Utility
    void handle_disconnect(std::shared_ptr<tcp::socket> socket);
    void safe_write(const std::shared_ptr<tcp::socket> &socket, const std::string &msg);
    void broadcast_room_update(int room_id);

    tcp::acceptor acceptor_;
    asio::io_context &io_context_;

    std::map<int, Room> active_rooms_;
    std::map<std::shared_ptr<tcp::socket>, Player> connected_players_;
    std::atomic<int> next_room_id_{0};
    std::atomic<int> next_player_id_num_{0};
    std::mutex mutex_;

    std::vector<std::thread> session_threads_;
    std::map<std::string, std::function<void(const json &, std::shared_ptr<tcp::socket>)>> request_handlers_;
};
