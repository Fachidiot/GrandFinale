
#pragma once

#include "stdafx.h"
#include "Server.h"

class Server; // 전방선언

class Session : public std::enable_shared_from_this<Session> // 비동기 콜백에서 shared_ptr를 안전하게 사용하기 위해 상속받는다.
{
public:
    Session(tcp::socket socket, Server &server);
    void start();
    void write(const std::string &msg);

private:
    void do_read();
    void do_write(const std::string &msg);

    tcp::socket socket_;                         // 소켓
    asio::streambuf buffer_;                     // 데이터 버퍼
    Server &server_;                             // 참조할 서버
    asio::strand<asio::any_io_executor> strand_; // 스트랜드
};
