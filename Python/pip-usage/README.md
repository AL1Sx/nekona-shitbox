# Pip Usage

Pip Usage 是一个用于分析已安装 Python 包磁盘使用情况的命令行工具。它可以扫描系统中已安装的 pip 包，并显示每个包占用的磁盘空间大小。

## 功能特性

- 扫描所有已安装的 pip 包
- 计算每个包占用的磁盘空间
- 支持彩色输出，便于阅读
- 多线程处理，提高扫描速度
- 支持按大小排序
- 提供多种输出格式（普通输出、JSON）
- 支持只显示最大的 N 个包

## 安装

首先确保你已经安装了所需依赖：

```bash
pip install -r requirements.txt
```

或者手动安装依赖：

```bash
pip install tqdm colorama
```

## 使用方法

### 基本用法

```bash
python main.py
```

这将扫描所有已安装的包并按大小降序显示它们的磁盘使用情况。

### 命令行选项

- `--top N` 或 `-t N`：只显示最大的 N 个包
- `--no-color`：禁用彩色输出
- `--json`：以 JSON 格式输出结果

### 示例

```bash
# 只显示最大的 10 个包
python main.py --top 10

# 以 JSON 格式输出结果
python main.py --json

# 禁用彩色输出
python main.py --no-color
```

## 输出格式

默认情况下，程序会显示每个包的名称、大小和安装路径。最后会显示统计摘要，包括找到的包数量和总大小。

当使用 `--json` 选项时，输出格式为 JSON，包含 `packages` 和 `summary` 两个部分。

## 依赖项

- Python 3.6+
- tqdm（可选，用于进度条）
- colorama（可选，用于彩色输出）