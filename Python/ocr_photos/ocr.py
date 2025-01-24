import os
from PIL import Image, ImageOps
import pytesseract
from tqdm import tqdm
from concurrent.futures import ThreadPoolExecutor, as_completed

# 设置Tesseract的路径
pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'

# 图片所在的目录
image_directory = 'Z:\图片集合\库存图片\音游成绩'
output_file = 'output.txt'

image_files = [f for f in os.listdir(image_directory) if f.lower().endswith(('.png', '.jpg', '.jpeg', '.gif', '.bmp'))]
total_images = len(image_files)

def process_image(filename):
    image_path = os.path.join(image_directory, filename)
    image = Image.open(image_path)
    image = image.convert('L')
    threshold = 128
    image = ImageOps.autocontrast(image, cutoff=0, ignore=None)
    image = image.point(lambda p: p < threshold and 255)
    text = pytesseract.image_to_string(image, lang='eng')
    return filename, text


with open(output_file, 'w', encoding='utf-8') as file, ThreadPoolExecutor() as executor:

    future_to_image = {executor.submit(process_image, filename): filename for filename in image_files}
    for future in tqdm(as_completed(future_to_image), total=total_images, desc="OCR进度", unit="张"):
        filename, text = future.result()
        file.write(f'文件名: {filename}\n{text}\n\n')

print('OCR完成,结果已保存至', output_file)