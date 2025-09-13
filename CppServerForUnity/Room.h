#pragma once

#include <string>
#include <vector>
#include <memory>
#include "asio.hpp"

// 방 정보를 담는 구조체
#pragma once

#include "stdafx.h"

// Forward declaration
class Session;

struct Room
{
    int id;
    std::string name;
    std::vector<std::shared_ptr<Session>> players;
    std::shared_ptr<Session> host = nullptr;
};
