import ctypes
import os
import zipfile
import requests
import tkinter as tk
from tkinter import filedialog, messagebox, ttk
from PIL import Image, ImageTk
import winshell
from win32com.client import Dispatch
import threading
import webbrowser
import pythoncom


class InstallerApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Linker 安装程序")
        self.root.geometry("400x400")
        self.root.resizable(False, False)  # 禁止调整窗口大小

        # 检查是否具有管理员权限
        if not self.is_admin():
            messagebox.showerror("权限不足", "当前用户没有管理员权限，请以管理员身份运行程序。")
            self.root.quit()  # 退出程序

        # 默认安装路径
        self.default_install_dir = r"C:\Program Files (x86)\linker"
        self.install_dir = self.default_install_dir

        # 设置背景色
        self.root.config(bg="#f4f4f4")

        # 加载并调整 logo 大小
        self.logo = Image.open("logo.png")  # 将 logo 图像
        self.logo = self.logo.resize((100, 100), Image.Resampling.LANCZOS)  # 调整 logo 的大小，100x100 你可以根据需要调整
        self.logo = ImageTk.PhotoImage(self.logo)

        self.logo_label = tk.Label(self.root, image=self.logo, bg="#f4f4f4")
        self.logo_label.pack(pady=40)

        # 快速安装按钮
        self.btn_install = tk.Button(self.root, text="快速安装", command=self.start_installation,
                                     bg="#007BFF", fg="white", font=("Arial", 14), width=20, height=2)
        self.btn_install.pack(pady=10)

        # 安装目录选择按钮
        self.btn_select_path = tk.Button(self.root, text="选择安装目录", command=self.select_install_path,
                                         bg="#007BFF", fg="white", font=("Arial", 10), width=20)
        self.btn_select_path.pack(pady=10)

        # 协议复选框和链接
        self.agree_var = tk.IntVar()
        self.agree_checkbox = tk.Checkbutton(self.root, text="我已阅读并同意", variable=self.agree_var,
                                             bg="#f4f4f4", font=("Arial", 10))
        self.agree_checkbox.pack(pady=10)

        # 用户协议链接
        self.terms_label = tk.Label(self.root, text="《用户许可协议》", fg="blue", cursor="hand2", bg="#f4f4f4")
        self.terms_label.pack(pady=5)
        self.terms_label.bind("<Button-1>", self.open_terms)  # 点击跳转到协议网页

        # 安装进度条
        self.progress = ttk.Progressbar(self.root, orient="horizontal", length=300, mode="determinate")
        self.progress.pack(pady=10)

        # 添加一个状态标签显示当前状态
        self.label_status = tk.Label(self.root, text="", bg="#f4f4f4", font=("Arial", 10))
        self.label_status.pack(pady=5)

    def is_admin(self):
        """检查当前程序是否以管理员身份运行"""
        try:
            return ctypes.windll.shell32.IsUserAnAdmin() != 0
        except:
            return False

    def select_install_path(self):
        self.install_dir = filedialog.askdirectory(initialdir=self.default_install_dir, title="选择安装目录")
        if not self.install_dir:  # 如果用户没有选择目录，则使用默认安装目录
            self.install_dir = self.default_install_dir

    def open_terms(self, event):
        # 打开用户许可协议的网页
        webbrowser.open("https://www.example.com/terms")  # 替换为实际的协议链接

    def start_installation(self):
        if not self.agree_var.get():
            messagebox.showerror("错误", "请先勾选‘我已阅读并同意《用户许可协议》’")
            return

        self.progress["value"] = 0
        self.btn_install.config(state=tk.DISABLED)  # 禁用按钮，防止重复点击

        # 启动安装线程
        threading.Thread(target=self.install_process, daemon=True).start()

    def install_process(self):
        # 确保安装路径存在
        zip_path = os.path.join(self.install_dir, "linker.zip")

        # 检查安装路径是否存在，不存在则创建
        if not os.path.exists(self.install_dir):
            os.makedirs(self.install_dir)

        # 下载 ZIP 文件
        self.update_status("正在下载 ZIP 文件...")
        self.download_zip("https://static.qbcode.cn/downloads/linker/v1.6.9/linker-win-x64.zip", zip_path)
        self.progress["value"] = 30

        # 解压 ZIP 文件
        self.update_status("正在解压文件...")
        extract_folder = self.extract_zip(zip_path, self.install_dir)
        self.progress["value"] = 70

        # 删除 ZIP 文件
        os.remove(zip_path)

        # 进入解压后的文件夹，检查 linker.tray.win.exe
        linker_exe = self.find_linker_exe(extract_folder)
        if linker_exe:
            self.update_status("创建快捷方式...")
            # 确保在子线程中正确调用 COM 库初始化
            pythoncom.CoInitialize()  # 初始化 COM 库
            self.create_shortcut(linker_exe, "linker")  # 快捷方式名称改为 linker
            self.progress["value"] = 100
            messagebox.showinfo("完成", "安装完成！")
        else:
            messagebox.showerror("错误", "linker.tray.win.exe 未找到，安装失败！")

        self.btn_install.config(state=tk.NORMAL)  # 重新启用按钮

    def download_zip(self, url, save_path):
        try:
            response = requests.get(url, stream=True)
            response.raise_for_status()  # 如果请求失败会抛出异常
            with open(save_path, 'wb') as file:
                for chunk in response.iter_content(1024):
                    file.write(chunk)
        except requests.exceptions.RequestException as e:
            messagebox.showerror("下载错误", f"下载失败: {e}")
            self.btn_install.config(state=tk.NORMAL)  # 重新启用按钮
            return

    def extract_zip(self, zip_path, extract_to):
        with zipfile.ZipFile(zip_path, 'r') as zip_ref:
            zip_ref.extractall(extract_to)
        # 返回解压文件夹路径
        return extract_to

    def find_linker_exe(self, extract_folder):
        # 遍历解压后的文件夹，寻找 linker.tray.win.exe
        for root, dirs, files in os.walk(extract_folder):
            if "linker.tray.win.exe" in files:
                return os.path.join(root, "linker.tray.win.exe")
        return None

    def create_shortcut(self, target_path, shortcut_name):
        desktop = winshell.desktop()
        shortcut_path = os.path.join(desktop, f"{shortcut_name}.lnk")
        shell = Dispatch('WScript.Shell')
        shortcut = shell.CreateShortcut(shortcut_path)
        shortcut.TargetPath = target_path
        shortcut.WorkingDirectory = os.path.dirname(target_path)
        shortcut.Save()

    def update_status(self, message):
        self.label_status.config(text=message)  # 更新状态标签文本
        self.root.update_idletasks()  # 更新 GUI

    def open_install_page(self):
        # 创建一个新的安装页面
        install_window = tk.Toplevel(self.root)
        install_window.title("安装中")
        install_window.geometry("600x400")

        # 使用 PanedWindow 控件分割页面
        paned_window = ttk.Panedwindow(install_window, orient="vertical")
        paned_window.pack(fill=tk.BOTH, expand=True)

        # 上部分显示网页，使用 Text 控件
        text_frame = ttk.Frame(paned_window)
        paned_window.add(text_frame, weight=2)
        web_text = tk.Text(text_frame, wrap=tk.WORD, height=20, bg="#f4f4f4")
        web_text.insert(tk.END, "正在加载网页内容...\n")
        web_text.pack(fill=tk.BOTH, expand=True)

        # 下部分显示进度条
        progress_frame = ttk.Frame(paned_window)
        paned_window.add(progress_frame, weight=1)
        progress = ttk.Progressbar(progress_frame, length=580, mode="indeterminate")
        progress.pack(pady=10)
        progress.start()

        # 模拟安装过程
        threading.Thread(target=self.simulate_install, args=(install_window, progress), daemon=True).start()

    def simulate_install(self, install_window, progress):
        # 模拟安装进度
        import time
        time.sleep(100)  # 假设安装过程需要 5 秒
        progress.stop()
        messagebox.showinfo("完成", "安装完成！")
        install_window.destroy()


if __name__ == "__main__":
    root = tk.Tk()
    app = InstallerApp(root)
    root.mainloop()
