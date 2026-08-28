import cv2

from calibration import pixel_to_machine


def detect_marks(img):
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    _, binary = cv2.threshold(
        gray,
        0,
        255,
        cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU
    )

    contours, _ = cv2.findContours(
        binary,
        cv2.RETR_EXTERNAL,
        cv2.CHAIN_APPROX_SIMPLE
    )

    results = []

    for contour in contours:
        area = cv2.contourArea(contour)

        if area < 1000:
            continue

        rect = cv2.minAreaRect(contour)
        (center_x, center_y), (width, height), raw_angle = rect

        angle = raw_angle

        if width < height:
            angle = raw_angle + 90

        if angle >= 90:
            angle -= 180

        machine_x, machine_y = pixel_to_machine(
            center_x,
            center_y
        )

        results.append({
            "x": round(center_x, 2),
            "y": round(center_y, 2),
            "machine_x": machine_x,
            "machine_y": machine_y,
            "angle": round(angle, 2),
            "area": round(area, 2)
        })

    results.sort(key=lambda item: item["x"])

    return results
