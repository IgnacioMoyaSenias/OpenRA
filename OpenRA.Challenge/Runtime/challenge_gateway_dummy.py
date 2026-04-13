#!/usr/bin/env python3
import json
import socket
import sys
import threading
import time


HOST = "127.0.0.1"
PORT1 = 50051
PORT2 = 50052


def send_jsonl(writer, payload):
    line = json.dumps(payload, separators=(",", ":")) + "\n"
    writer.write(line.encode("utf-8"))
    writer.flush()


def handle_client(conn, addr, port):
    print(f"[dummy] client connected from {addr} on port {port}", flush=True)

    with conn:
        reader = conn.makefile("r", encoding="utf-8", newline="\n")
        writer = conn.makefile("wb")
        for line in reader:
            line = line.strip()
            if not line:
                continue

            msg = json.loads(line)
            print(f"[dummy] <= {msg.get('type')} {line} (port {port})", flush=True)

            if msg.get("type") == "observation":
                response = {
                    "type": "actions",
                    "api_version": "v1",
                    "match_id": msg["match_id"],
                    "player_id": msg["player_id"],
                    "decision_id": msg["decision_id"],
                    "actions": []
                }
                send_jsonl(writer, response)
                print(f"[dummy] => actions decision_id={msg['decision_id']} (port {port})", flush=True)

            if msg.get("type") == "end":
                print(f"[dummy] received end on port {port}; closing connection", flush=True)
                break


def start_server(port):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server.bind((HOST, port))
        server.listen(1)
        print(f"[dummy] listening on tcp:{HOST}:{port}", flush=True)

        while True:
            conn, addr = server.accept()
            client_thread = threading.Thread(target=handle_client, args=(conn, addr, port))
            client_thread.daemon = True
            client_thread.start()


def main():
    global PORT1, PORT2

    if len(sys.argv) >= 2:
        PORT1 = int(sys.argv[1])
    if len(sys.argv) >= 3:
        PORT2 = int(sys.argv[2])

    print(f"[dummy] starting dual-port gateway on ports {PORT1} and {PORT2}", flush=True)

    # Start servers in separate threads
    server1_thread = threading.Thread(target=start_server, args=(PORT1,))
    server2_thread = threading.Thread(target=start_server, args=(PORT2,))

    server1_thread.daemon = True
    server2_thread.daemon = True

    server1_thread.start()
    server2_thread.start()

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("[dummy] shutting down", flush=True)


if __name__ == "__main__":
    main()
