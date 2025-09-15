#pragma once

#include "stdafx.h"
#include "Player.h"
#include "Room.h"
#include <asio/ip/udp.hpp> // UDP 헤더 추가

// 네임스페이스 선언 추가
using asio::ip::udp;

// Forward declaration of Session class
class Session;

class Server
{
public:
    // Constructor now accepts both TCP and UDP ports
    Server(asio::io_context& io_context, short tcp_port, short udp_port);
    void run();

    // Game Loop
    void start_game_loop();
    void tick();

    // Interface for Session class (TCP)
    void handle_connect(std::shared_ptr<Session> session);
    void handle_disconnect(std::shared_ptr<Session> session);
    void handle_request(std::shared_ptr<Session> session, const std::string& message);

private:
    // TCP Methods
    void start_accept();

    // UDP Methods
    void start_udp_receive();
    void handle_udp_receive(const asio::error_code& error, std::size_t bytes_transferred);

    // Request Handlers (TCP)
    void initialize_request_handlers();
    void handle_create_room(std::shared_ptr<Session> session, const json& req);
    void handle_find_rooms(std::shared_ptr<Session> session, const json& req);
    void handle_join_room(std::shared_ptr<Session> session, const json& req);
    void handle_chat_message(std::shared_ptr<Session> session, const json& req);
    void handle_leave_room(std::shared_ptr<Session> session, const json& req);
    void handle_toggle_ready(std::shared_ptr<Session> session, const json& req);
    void handle_start_game(std::shared_ptr<Session> session, const json& req);
    void handle_set_nickname(std::shared_ptr<Session> session, const json& req);
    void handle_player_action(std::shared_ptr<Session> session, const json& req); // 액션 핸들러 추가

    // Utility
    void broadcast_room_update(int room_id);

    // ASIO and Networking members
    asio::io_context& io_context_;
    asio::strand<asio::io_context::executor_type> server_strand_;
    std::vector<std::thread> thread_pool_;

    // TCP members
    tcp::acceptor acceptor_;

    // UDP members
    udp::socket udp_socket_;
    udp::endpoint remote_udp_endpoint_;
    std::array<char, 1024> udp_buffer_;

    // Game State members
    asio::steady_timer game_loop_timer_;
    const std::chrono::milliseconds tick_interval_{50}; // 20 ticks per second

    std::map<int, Room> active_rooms_;
    std::map<std::shared_ptr<Session>, Player> connected_players_;
    std::map<std::string, udp::endpoint> player_udp_endpoints_; // Maps player_id to their UDP endpoint

    std::atomic<int> next_room_id_{0};
    std::atomic<int> next_player_id_num_{0};

    std::map<std::string, std::function<void(std::shared_ptr<Session>, const json&)>> request_handlers_;
};

