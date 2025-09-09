#include "stdafx.h"
#include "Server.h"

int main() {
    try {
        asio::io_context io_context;
        Server server(io_context, 8080);
        server.run();
    } catch (std::exception& e) {
        std::cerr << "Exception: " << e.what() << std::endl;
    }

    return 0;
}
