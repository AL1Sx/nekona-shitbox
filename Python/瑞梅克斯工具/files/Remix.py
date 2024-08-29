import time
import base64
import os
import sys
import threading

print("RemixTools\n版本号:V1.2.0\n键入[H]以获取命令的帮助,请在出现[>>>]后再键入相关命令.\n---")
sys.path.append(os.path.join(os.path.dirname(__file__), 'Remix_functions'))
from Remix_functions import search_engine, file_hash_check, nano_hash_check, print_history

#头
while True:
    time.sleep(0)
    insert = input("主页>>>")
    if 'H' in insert or 'h' in insert:
        print('=========')
        print('菜单')
        print('=========')
        print('[ I ]互联网工具 包含:')
        print('   ->简易互联网搜索引擎.')
        print('---------')
        print('[ C ]简易校验工具 包含:')
        print('   ->检查字符串格式数据.')
        print('   ->文件/字符串校验.')
        print('=========')
        print('[ H ]帮助菜单.')
        #print('[ P ]本次使用历史记录.')
        print('[ A ]关于本程序.')
        print('[ Q ]退出本程序.\n---')

# 互联网工具
    elif 'I' in insert or 'i' in insert:
        print('请选择你所想要的互联网工具:')
        print('[S]简易互联网搜索引擎.')
        print('[任意]返回')
        choice = input("选择>>>")
        print('---')
        if 'S' in choice or 's' in choice:
            print('请选择你所想要的搜索引擎:\n[B]必应 [G]谷歌 [任意]返回 [QUIT]在搜索功能内退出\n---')
            search_choice = input("选择>>>")
            print('')
            while True:
                if 'B' in search_choice or 'b' in search_choice:
                    if not search_engine('必应', 'https://www.bing.com/search?q='):
                        break
                elif 'G' in search_choice or 'g' in search_choice:
                    if not search_engine('谷歌', 'https://www.google.com/search?q='):
                        break

# 简易校验工具
    elif 'C' in insert or 'c' in insert:
        print('请选择你所想要的简易校验工具:')
        print('[E]检查字符串Base64/格式数据.')
        print('[M]使用文件/字符串校验工具.')
        print('[任意]返回')
        choice = input("选择>>>")
        print('---')
        
        if 'E' in choice or 'e' in choice:
            print('请选择你所想要的字符转换功能:')
            print('[B]Base64转换 [M]检查多个字符 [任意]返回 [QUIT]在功能内退出')
            conversion_choice = input('选择>>>')
            print('---')
            while True:
                if 'B' in conversion_choice or 'b' in conversion_choice:
                    text = input('输入文本>>>')
                    print('---')
                    if text == 'QUIT':
                        print('已退出转换功能.\n---')
                        break
                    else:
                        encoded = base64.b64encode(text.encode('utf-8')).decode('utf-8')
                        print('转换后的Base64编码:', encoded,'\n---')
                elif 'M' in conversion_choice or 'm' in conversion_choice:
                    words = input('输入字符>>>')
                    print('---')
                    if words == 'QUIT':
                        print('已退出转换功能.\n---')
                        break
                    else:
                        for word in words:
                            print('字符:', word, 'ASCII:', ord(word), 'Hex:', hex(ord(word)), '\n---')
        
        elif 'M' in choice or 'm' in choice:
            print('请选择你所想要的校验工具:')
            print('[F]文件校验 [N]字符串校验 [任意]返回 [QUIT]在功能内退出')
            check_choice = input('选择>>>')
            print('---')
            while True:
                if 'F' in check_choice or 'f' in check_choice:
                    file_path = input('请输入文件路径>>>')
                    if file_path.upper() == 'QUIT':
                        print('已退出校验工具.\n---')
                        break
                    hash_type = input('请输入哈希类型(MD5, SHA1, SHA224, SHA256, SHA384, SHA512)>>>')
                    if hash_type.upper() == 'QUIT':
                        print('已退出校验工具.\n---')
                        break
                    file_hash_check(file_path, hash_type)
                elif 'N' in check_choice or 'n' in check_choice:
                    nano_check = input('请输入字符串>>>')
                    if nano_check.upper() == 'QUIT':
                        print('已退出校验工具.\n---')
                        break
                    hash_type = input('请输入哈希类型(MD5, SHA1, SHA224, SHA256, SHA384, SHA512)>>>')
                    if hash_type.upper() == 'QUIT':
                        print('已退出校验工具.\n---')
                        break
                    nano_hash_check(nano_check, hash_type)
                else:
                    print('返回主菜单.\n---')
                    break

    elif 'P' in insert or 'p' in insert:
        print_history()

    elif 'A' in insert or 'a' in insert:
        print('RemixTools/瑞梅克斯工具 版本号: V1.2.0\n')

    elif 'Q' in insert or 'q' in insert:
        print('已退出本程序.')
        break