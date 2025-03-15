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
from io import BytesIO
import sys


class InstallerApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Linker 安装程序")
        self.root.geometry("400x400")
        self.root.resizable(False, False)  # 禁止调整窗口大小

        # 检查管理员权限
        if not self.is_admin():
            messagebox.showerror("权限不足", "当前用户没有管理员权限，请以管理员身份运行程序。")
            self.root.quit()

        # 默认安装路径
        self.default_install_dir = r"C:\Program Files (x86)\linker"
        self.install_dir = self.default_install_dir

        # 设置背景色
        self.root.config(bg="#f4f4f4")

        # 加载 Logo
        self.load_logo()

        # 快速安装按钮
        self.btn_install = tk.Button(self.root, text="快速安装", command=self.start_installation,
                                     bg="#007BFF", fg="white", font=("Arial", 14), width=20, height=2)
        self.btn_install.pack(pady=10)

        # 安装目录选择按钮
        self.btn_select_path = tk.Button(self.root, text="选择安装目录", command=self.select_install_path,
                                         bg="#007BFF", fg="white", font=("Arial", 10), width=20)
        self.btn_select_path.pack(pady=10)

        # 协议复选框
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

        # 状态标签
        self.label_status = tk.Label(self.root, text="", bg="#f4f4f4", font=("Arial", 10))
        self.label_status.pack(pady=5)

    def is_admin(self):
        """检查当前程序是否以管理员身份运行"""
        try:
            return ctypes.windll.shell32.IsUserAnAdmin() != 0
        except:
            return False

    def load_logo(self):
        """加载 logo（本地优先，失败后从网络加载）"""
        if getattr(sys, 'frozen', False):
            base_path = sys._MEIPASS  # PyInstaller 运行环境
        else:
            base_path = os.path.dirname(__file__)  # 普通 Python 运行环境

        local_logo_path = os.path.join(base_path, 'img', 'logo.png')
        remote_logo_url = "https://linker-doc.snltty.com/img/logo.png"

        try:
            if os.path.exists(local_logo_path):
                self.logo = Image.open(local_logo_path)
            else:
                response = requests.get(remote_logo_url, timeout=10)
                response.raise_for_status()
                img_data = BytesIO(response.content)
                self.logo = Image.open(img_data)

            # 调整大小
            self.logo = self.logo.resize((100, 100), Image.Resampling.LANCZOS)
            self.logo = ImageTk.PhotoImage(self.logo)
            self.logo_label = tk.Label(self.root, image=self.logo, bg="#f4f4f4")
            self.logo_label.pack(pady=20)  # 确保 Logo 正确显示

        except Exception as e:
            print(f"加载 logo 失败: {e}")
            self.logo = None

    def select_install_path(self):
        """选择安装路径"""
        self.install_dir = filedialog.askdirectory(initialdir=self.default_install_dir, title="选择安装目录")
        if not self.install_dir:
            self.install_dir = self.default_install_dir

    def open_terms(self, event):
        """打开用户许可协议"""
        webbrowser.open("https://linker-doc.snltty.com/docs/1%E3%80%81%E9%A6%96%E9%A1%B5")

    def start_installation(self):
        """开始安装"""
        if not self.agree_var.get():
            messagebox.showerror("错误", "请先勾选‘我已阅读并同意《用户许可协议》’")
            return

        self.progress["value"] = 0
        self.btn_install.config(state=tk.DISABLED)

        # 启动安装
        threading.Thread(target=self.install_process, daemon=True).start()

    def install_process(self):
        """执行安装流程"""
        zip_path = os.path.join(self.install_dir, "linker.zip")

        if not os.path.exists(self.install_dir):
            os.makedirs(self.install_dir)

        self.update_status("正在下载 ZIP 文件...")
        self.download_zip("https://static.qbcode.cn/downloads/linker/v1.6.9/linker-win-x64.zip", zip_path)
        self.progress["value"] = 30

        self.update_status("正在解压文件...")
        extract_folder = self.extract_zip(zip_path, self.install_dir)
        self.progress["value"] = 70

        os.remove(zip_path)

        linker_exe = self.find_linker_exe(extract_folder)
        if linker_exe:
            self.update_status("创建快捷方式...")
            pythoncom.CoInitialize()
            self.create_shortcut(linker_exe, "linker")
            self.progress["value"] = 100
            messagebox.showinfo("完成", "安装完成！")
        else:
            messagebox.showerror("错误", "linker.tray.win.exe 未找到，安装失败！")

        self.btn_install.config(state=tk.NORMAL)

    def download_zip(self, url, save_path):
        """下载 ZIP 文件"""
        try:
            response = requests.get(url, stream=True)
            response.raise_for_status()
            with open(save_path, 'wb') as file:
                for chunk in response.iter_content(1024):
                    file.write(chunk)
        except requests.exceptions.RequestException as e:
            messagebox.showerror("下载错误", f"下载失败: {e}")
            self.btn_install.config(state=tk.NORMAL)

    def extract_zip(self, zip_path, extract_to):
        """解压 ZIP 文件"""
        with zipfile.ZipFile(zip_path, 'r') as zip_ref:
            zip_ref.extractall(extract_to)
        return extract_to

    def find_linker_exe(self, extract_folder):
        """寻找 linker.tray.win.exe"""
        for root, dirs, files in os.walk(extract_folder):
            if "linker.tray.win.exe" in files:
                return os.path.join(root, "linker.tray.win.exe")
        return None

    def create_shortcut(self, target_path, shortcut_name):
        """创建快捷方式"""
        desktop = winshell.desktop()
        shortcut_path = os.path.join(desktop, f"{shortcut_name}.lnk")
        shell = Dispatch('WScript.Shell')
        shortcut = shell.CreateShortcut(shortcut_path)
        shortcut.TargetPath = target_path
        shortcut.WorkingDirectory = os.path.dirname(target_path)
        shortcut.Save()

    def update_status(self, message):
        """更新状态标签"""
        self.label_status.config(text=message)
        self.root.update_idletasks()


if __name__ == "__main__":
    root = tk.Tk()
    app = InstallerApp(root)
    root.mainloop()
