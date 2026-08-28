import cv2

from vision import detect_mark


# 读取图片
img = cv2.imread("images/rotated_rect.jpg")

if img is None:
    print("图片读取失败")
    exit()


# 调用我们自己的视觉函数
result = detect_mark(img)


# 判断有没有检测成功
if result is None:

    print("没有检测到目标")

else:

    print("检测成功")

    print("X =", round(result["x"], 2))
    print("Y =", round(result["y"], 2))
    print("Angle =", round(result["angle"], 2))