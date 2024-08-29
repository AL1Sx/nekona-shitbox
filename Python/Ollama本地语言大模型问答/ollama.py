import requests
import json

# 记得自定义服务器IP和端口
server_ip = '127.0.0.1'  # 替换为实际的OLLAMA服务器IP
server_port = 11434  # 替换为实际的OLLAMA服务器端口号

while True:
    question = input('>>>')
    if question == 'QUIT()': # 退出循环用
        break
    elif question == 'ABOUT()':
        print("By Nekona Alice")

    url = f'http://{server_ip}:{server_port}/api/generate'

    headers = {'Content-Type': 'application/json'}

    payload = {
        'model': 'Codeqwen1-5-7b:latest',  # OLLAMA模型名称，根据实际情况替换
        'prompt': question,
    }

    try:
        response = requests.post(url, json=payload, headers=headers, stream=True)
        response.raise_for_status()

        full_response = ""
        total_received = 0  # 用于记录已接收的字符数

        for line in response.iter_lines():
            if line:
                data = json.loads(line.decode('utf-8'))

                # 更新总接收的字符数
                total_received += len(data['response'])

                full_response += data['response']

            print(f'\r正在生成中: {total_received} 字符已处理', end='', flush=True)

        print('\n')

        print('-----------------------------------------')
        print("完整回答：", full_response)
        print('-----------------------------------------')

    except requests.exceptions.RequestException as e:
        print(f'请求失败: {e}')