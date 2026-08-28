import cv2
import numpy as np

# 创建白色背景
img = np.ones(
    (600, 800, 3),
    dtype=np.uint8
) * 255


def draw_rotated_rect(
    image,
    center,
    size,
    angle
):
    rect = (
        center,
        size,
        angle
    )

    box = cv2.boxPoints(rect)
    box = box.astype(int)

    cv2.fillPoly(
        image,
        [box],
        (0, 0, 0)
    )


# 第一个目标
draw_rotated_rect(
    img,
    (200, 180),
    (180, 70),
    20
)

# 第二个目标
draw_rotated_rect(
    img,
    (500, 200),
    (160, 60),
    -30
)

# 第三个目标
draw_rotated_rect(
    img,
    (420, 430),
    (220, 80),
    45
)


cv2.imwrite(
    "images/multi_marks.jpg",
    img
)

print("多目标测试图片生成完成")