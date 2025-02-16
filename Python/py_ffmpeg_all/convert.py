import os
import subprocess
from tqdm import tqdm
from shutil import copyfile

# 设置 FFmpeg 的完整路径
ffmpeg_path = r'X:\ffmpeg.exe'

# 设置输入和输出文件夹路径
input_folder = r'\\Server\XXXX'  # 输入文件夹路径
output_folder = r'C:\Output'  # 输出文件夹路径
crf_value = 22  # 设置CRF值，默认23
preset = 'medium'  # 设置编码预设

# 确保输出文件夹存在
if not os.path.exists(output_folder):
    os.makedirs(output_folder)

# 获取输入文件夹中的视频文件列表
video_files = [f for f in os.listdir(input_folder) if f.endswith(('.mp4', '.mkv', '.avi'))]
total_videos = len(video_files)

# 使用 tqdm 创建进度条
for filename in tqdm(video_files, desc="Processing", unit="file"):
    inputfile = os.path.join(input_folder, filename)
    outputfile = os.path.join(output_folder, 'zip_' + filename)
    
    # 构建FFmpeg命令
    command = [
        ffmpeg_path,
        '-i', inputfile,
        '-c:v', 'libx264',
        '-crf', str(crf_value),
        '-preset', preset,
        '-c:a', 'aac',
        '-b:a', '128k',  # 设置音频比特率
        '-map_metadata', '0',  # 保留元数据
        '-y',  # 允许覆盖输出文件
        outputfile
    ]
    
    # 执行FFmpeg命令
    subprocess.run(command, check=True)
    
    # 复制原始文件的修改日期到新文件
    stat = os.stat(inputfile)
    os.utime(outputfile, (stat.st_atime, stat.st_mtime))

print("视频转换完成。")
