from calibration import pixel_to_machine

points = [
    (199.5, 179.5),
    (419.5, 429.5),
    (499.5, 199.5)
]

print("==============================")
print(" 9 Point Calibration Test")
print("==============================")

for index, (pixel_x, pixel_y) in enumerate(points, start=1):
    machine_x, machine_y = pixel_to_machine(pixel_x, pixel_y)
    print()
    print(f"Mark {index}")
    print(f"Pixel   X = {pixel_x:.2f}")
    print(f"Pixel   Y = {pixel_y:.2f}")
    print(f"Machine X = {machine_x:.3f} mm")
    print(f"Machine Y = {machine_y:.3f} mm")
