import os

import subprocess

import sys

from typing import Optional, List, Tuple, Dict

from concurrent.futures import ThreadPoolExecutor, as_completed

import argparse



try:

    from tqdm import tqdm

except Exception:

    tqdm = None



try:

    from colorama import init, Fore, Style

    init(autoreset=True)

except Exception:

    Fore = None

    Style = None



def human_readable(size: float) -> str:

    for unit in ('B','KB','MB','GB','TB'):

        if size < 1024.0:

            return f"{size:3.1f}{unit}"

        size /= 1024.0

    return f"{size:.1f}PB"





def get_path_size(path: str) -> int:

    if os.path.isfile(path):

        try:

            return os.path.getsize(path)

        except OSError:

            return 0

    total = 0

    for root, _, files in os.walk(path, onerror=lambda e: None):

        for f in files:

            fp = os.path.join(root, f)

            try:

                total += os.path.getsize(fp)

            except OSError:

                pass

    return total





def _color(text: str, color: Optional[str]) -> str:

    if Fore is None or color is None:

        return text

    return f"{color}{text}{Style.RESET_ALL}"





def _find_package_path(location: str, package_name: str) -> Optional[str]:

    package_path = None

    if not location:

        return None



    candidates = [

        os.path.join(location, package_name),

        os.path.join(location, package_name.replace('-', '_')),

        os.path.join(location, package_name + '.py'),

    ]

    try:

        for item in os.listdir(location):

            lower = item.lower()

            if lower.startswith(package_name.replace('-', '_').lower()) and (lower.endswith('.dist-info') or lower.endswith('.egg-info')):

                candidates.append(os.path.join(location, item))

    except OSError:

        pass



    for c in candidates:

        if os.path.exists(c):

            return c



    try:

        matches = [os.path.join(location, d) for d in os.listdir(location) if package_name.lower() in d.lower()]

        if matches:

            return matches[0]

    except OSError:

        pass



    return None





def _process_package(package_name: str, pip_cmd: List[str], env: Dict[str, str]) -> Tuple[str, Optional[int], Optional[str], Optional[str]]:

    try:

        package_info = subprocess.check_output(

            pip_cmd + ['show', package_name], env=env, text=True, encoding='utf-8', errors='replace'

        )

        location = None

        for line in package_info.splitlines():

            if line.startswith('Location:'):

                location = line.split(':', 1)[1].strip()

                break



        package_path = _find_package_path(location, package_name)



        if package_path and os.path.exists(package_path):

            size = get_path_size(package_path)

            return (package_name, size, package_path, None)

        else:

            return (package_name, None, None, 'Path not found')

    except subprocess.CalledProcessError as e:

        return (package_name, None, None, f'Error retrieving package info: {str(e)}')

    except Exception as e:

        return (package_name, None, None, f'Unexpected error: {str(e)}')





def scan_packages(show_all: bool = False, top_n: int = None, sort_by_size: bool = True) -> Tuple[List[Tuple[str, Optional[int], Optional[str], Optional[str]]], List[Tuple[str, Optional[int], Optional[str], Optional[str]]]]:

    pip_cmd = [sys.executable, '-m', 'pip']

    env = os.environ.copy()

    env['PYTHONUTF8'] = '1'

    env['PYTHONIOENCODING'] = 'utf-8'

    installed_packages = subprocess.check_output(

        pip_cmd + ['list', '--format=freeze'], env=env, text=True, encoding='utf-8', errors='replace'

    ).splitlines()

    package_names = [pkg.split('==')[0] for pkg in installed_packages if pkg.strip()]



    # 并行处理以加快速度

    workers = min(32, (os.cpu_count() or 1) * 5)

    if tqdm:

        pbar = tqdm(total=len(package_names), desc="Scanning", unit="pkg")

    else:

        pbar = None



    results = []

    with ThreadPoolExecutor(max_workers=workers) as ex:

        futures = {ex.submit(_process_package, name, pip_cmd, env): name for name in package_names}

        try:

            if pbar:

                with pbar:

                    for fut in as_completed(futures):

                        result = fut.result()

                        results.append(result)

                        name, size, package_path, err = result

                        if size is not None:

                            # 分别设置包名(绿色)、大小(白色)、路径(灰色)的颜色

                            colored_name = _color(name, Fore.GREEN if Fore else None)

                            colored_size = _color(human_readable(size), Fore.WHITE if Fore else None)

                            colored_path = _color(f"({package_path})", Fore.LIGHTBLACK_EX if Fore else None)

                            line = f"{colored_name}: {colored_size}  {colored_path}"

                            colored = line

                        else:

                            line = f"{name}: {err}"

                            colored = _color(line, Fore.YELLOW if Fore else None) if err == 'Path not found' else _color(line, Fore.RED if Fore else None)

                        if show_all:

                            if tqdm:

                                tqdm.write(colored)

                            else:

                                print(colored)

                        pbar.update(1)

            else:

                for fut in as_completed(futures):

                    result = fut.result()

                    results.append(result)

                    name, size, package_path, err = result

                    if size is not None:

                        # 分别设置包名(绿色)、大小(白色)、路径(灰色)的颜色

                        colored_name = _color(name, Fore.GREEN if Fore else None)

                        colored_size = _color(human_readable(size), Fore.WHITE if Fore else None)

                        colored_path = _color(f"({package_path})", Fore.LIGHTBLACK_EX if Fore else None)

                        line = f"{colored_name}: {colored_size}  {colored_path}"

                        colored = line

                    else:

                        line = f"{name}: {err}"

                        colored = _color(line, Fore.YELLOW if Fore else None) if err == 'Path not found' else _color(line, Fore.RED if Fore else None)

                    if show_all:

                        print(colored)

        except KeyboardInterrupt:

            for f in futures:

                f.cancel()

            raise



    # 过滤出有效的包大小结果并排序

    valid_results = [(name, size, path, err) for name, size, path, err in results if size is not None]

    if sort_by_size:

        valid_results.sort(key=lambda x: x[1], reverse=True)  # 按大小降序排序



    # 如果指定了top_n，则只返回前n个

    if top_n:

        valid_results = valid_results[:top_n]



    return results, valid_results





def main():

    parser = argparse.ArgumentParser(description='显示已安装pip包的磁盘使用情况')

    parser.add_argument('--top', '-t', type=int, help='显示最大的N个包')

    parser.add_argument('--no-color', action='store_true', help='禁用彩色输出')

    parser.add_argument('--json', action='store_true', help='以JSON格式输出结果')

    args = parser.parse_args()



    global Fore, Style

    if args.no_color:

        Fore = None

        Style = None



    results, valid_results = scan_packages(show_all=not args.json, top_n=args.top)



    # 计算总数和总大小

    total_size = sum(size for _, size, _, _ in valid_results)

    total_count = len(valid_results)



    if args.json:

        import json

        output = {

            'packages': [{'name': name, 'size': size, 'path': path} for name, size, path, _ in valid_results],

            'summary': {

                'total_packages': total_count,

                'total_size_bytes': total_size,

                'total_size_human': human_readable(total_size)

            }

        }

        print(json.dumps(output, indent=2, ensure_ascii=False))

    else:

        sep = "=" * 60

        if Fore:

            print(_color(sep, Fore.CYAN))

            # 使用绿色显示包数量信息

            packages_text = _color(f"Packages found & counted: ", Fore.GREEN)

            count_text = _color(f"{total_count}/{len(results)}", Fore.WHITE)

            print(f"{packages_text}{count_text}")

            # 使用绿色显示"Total size"，白色显示实际大小

            size_label = _color("Total size: ", Fore.GREEN)

            size_value = _color(human_readable(total_size), Fore.WHITE)

            print(f"{size_label}{size_value}")

        else:

            print(sep)

            print(f"Packages found & counted: {total_count}/{len(results)}")

            print(f"Total size: {human_readable(total_size)}")





if __name__ == "__main__":

    main()