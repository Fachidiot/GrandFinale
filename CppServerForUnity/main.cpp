#include "stdafx.h"
#include "Server.h"

int main() {
    try {
        asio::io_context io_context;
        // Pass both TCP and UDP ports to the Server constructor
        Server server(io_context, 8080, 8081);
        server.run();
    } catch (std::exception& e) {
        std::cerr << "Exception: " << e.what() << std::endl;
    }

    return 0;
}
