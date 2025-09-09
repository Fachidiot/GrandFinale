#pragma once

#include <string>
#include <vector>
#include <memory>
#include "asio.hpp"

// 방 정보를 담는 구조체
struct Room
{
    int id;
    std::string name;
    std::vector<std::shared_ptr<asio::ip::tcp::socket>> players;
    std::shared_ptr<asio::ip::tcp::socket> host_socket = nullptr; // 방장 소켓.
};
