#include "Session.h"
#include "Server.h"

Session::Session(tcp::socket socket, Server &server)
    : socket_(std::move(socket)), server_(server), strand_(socket_.get_executor()) {}

void Session::start()
{
    // 서버에 새로운 세션이 시작됐음을 알려준다.
    server_.handle_connect(shared_from_this());
    do_read();
}

void Session::do_read()
{
    auto self = shared_from_this();
    asio::async_read_until(socket_, buffer_, "\n", asio::bind_executor(strand_, [this, self](const asio::error_code &ec, std::size_t length)
                                                                       {
    if (!ec)
    {
        std::istream is(&buffer_);
        std::string message(std::istreambuf_iterator<char>(is), {});
        // The message includes the delimiter, so we might want to remove it
        message.pop_back(); // Remove '\n'

        server_.handle_request(self, message);
        do_read(); // Continue reading the next message
    }
    else
    {
        server_.handle_disconnect(self);
    } }));
}

void Session::do_write(const std::string &msg)
{
    auto self = shared_from_this();
    auto buffer = std::make_shared<std::string>(msg + "\n");
    asio::async_write(socket_, asio::buffer(*buffer), asio::bind_executor(strand_, [this, self, buffer](const asio::error_code &ec, std::size_t /*length*/)
                                                                          {
    if (ec)
    {
        server_.handle_disconnect(self);
    } }));
}

// This public-facing write function can be called from outside the Session class
// It posts the actual write operation to the strand to maintain thread safety.
void Session::write(const std::string &msg)
{
    asio::post(strand_, [this, self = shared_from_this(), msg]()
               { do_write(msg); });
}
