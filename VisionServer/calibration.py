import cv2
import numpy as np

PIXEL_POINTS = np.array([
    [100, 100], [400, 100], [700, 100],
    [100, 300], [400, 300], [700, 300],
    [100, 500], [400, 500], [700, 500]
], dtype=np.float32)

MACHINE_POINTS = np.array([
    [10, 10], [40, 10], [70, 10],
    [10, 30], [40, 30], [70, 30],
    [10, 50], [40, 50], [70, 50]
], dtype=np.float32)

def build_calibration_matrix():
    matrix, _ = cv2.estimateAffine2D(PIXEL_POINTS, MACHINE_POINTS)
    if matrix is None:
        raise RuntimeError("标定失败：无法计算 Pixel -> Machine 变换矩阵")
    return matrix

CALIBRATION_MATRIX = build_calibration_matrix()

def pixel_to_machine(pixel_x, pixel_y):
    pixel = np.array([pixel_x, pixel_y, 1.0], dtype=np.float64)
    machine = CALIBRATION_MATRIX @ pixel
    return round(float(machine[0]), 3), round(float(machine[1]), 3)

if __name__ == "__main__":
    print("=== 9 点标定 ===")
    print(CALIBRATION_MATRIX)
    for x, y in [(199.5,179.5),(419.5,429.5),(499.5,199.5)]:
        mx, my = pixel_to_machine(x, y)
        print(f"Pixel ({x:.2f}, {y:.2f}) -> Machine ({mx:.3f} mm, {my:.3f} mm)")
