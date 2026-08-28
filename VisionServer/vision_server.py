import socket
import json
import cv2

from vision import detect_marks

HOST = "0.0.0.0"
PORT = 5001

server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server.bind((HOST, PORT))
server.listen(5)

print("==============================")
print(" Vision Server 已启动")
print(" Port:", PORT)
print("==============================")

while True:
    client = None

    try:
        client, address = server.accept()

        print()
        print("客户端已连接:", address)

        data = client.recv(1024)

        if not data:
            continue

        message = data.decode("utf-8").strip()

        print("收到命令:", message)

        if message == "detect":
            img = cv2.imread("images/multi_marks.jpg")

            if img is None:
                response = {
                    "success": False,
                    "message": "图片读取失败"
                }

            else:
                marks = detect_marks(img)

                if len(marks) == 0:
                    response = {
                        "success": False,
                        "message": "没有检测到目标"
                    }

                else:
                    response = {
                        "success": True,
                        "count": len(marks),
                        "marks": marks
                    }

        else:
            response = {
                "success": False,
                "message": "未知命令"
            }

        response_json = json.dumps(response)

        client.sendall(
            response_json.encode("utf-8")
        )

        print("发送结果:", response_json)

    except Exception as e:
        print("发生错误:", e)

    finally:
        if client is not None:
            client.close()

        print("等待下一次检测...")