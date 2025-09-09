#pragma once

// C++ Standard Library
#include <iostream>
#include <string>
#include <vector>
#include <map>
#include <mutex>
#include <thread>
#include <functional>
#include <memory>
#include <atomic>
#include <algorithm>

// Third-party Libraries
#include "asio.hpp"
#include "nlohmann/json.hpp"

// Common using declarations
using asio::ip::tcp;
using json = nlohmann::json;
