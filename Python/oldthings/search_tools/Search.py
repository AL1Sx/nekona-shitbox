# _*_ coding : UTF-8;CRLF
# Developers : Nekona;HuiChen
# Time       : 2023-11-26
# File Name  : Search.py
# Develop Tool : Python
import webbrowser
import time

print("搜整工具")
print("版本号:Preview0")
print("键入[H]以获取支持的搜索引擎列表,请在出现[>>>]后再键入相关命令.")

while True:
    time.sleep(1)
    insert = input("主页>>>")
    if 'H' in insert or 'h' in insert:
        print('==可支持搜索引擎列表==')
        print('')
        print('<一般网页搜索>')
        print('[ B ]必应搜索')
        print('[ G ]谷歌搜索')
        print('[DU ]百度搜索')
        print('')
        print('<视频网站搜索>')
        print('[BI ]哔站搜索')
        print('[BV ]哔站定向')
        print('[ Y ]油管搜索')
        print('')
        print('<其他网站搜索>')
        print('[ P ]Pixiv搜索')
        print('[PID]P站图片定向')
        print('[PND]P站小说定向')
        print('')
        print('<按[A]查看关于,[Q]退出程序>')


    elif 'B' in insert or 'b' in insert:
        word = input('必应搜索>>>')
        if 'QUIT' in word:
            print('已退出搜索功能.')
            print('')
            break
        else:
            print('搜索:' + word )
            url = 'https://www.bing.com/search?q=' + word
            webbrowser.open(url, new=0, autoraise=True)
    







    #退出
    elif 'Q' in insert or 'q' in insert:
        print('即将在2秒后退出,[Enter]取消退出(没做)')
        time.sleep(2)
        break

#判定
    else:
        print('错误的')