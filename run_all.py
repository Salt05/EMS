import subprocess
import time
import sys
import webbrowser
import os

# Ensure stdout supports UTF-8 for Vietnamese characters and symbols
if sys.version_info >= (3, 7):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def kill_stale_processes():
    print("[CLEAN] Dang quet sach cac tien trinh cu de giai phong cong (EMS.Mvc, EMS.WebAPI)...")
    if sys.platform == "win32":
        os.system("taskkill /F /IM EMS.Mvc.exe >nul 2>&1")
        os.system("taskkill /F /IM EMS.WebAPI.exe >nul 2>&1")
    else:
        os.system("pkill -f EMS.Mvc >/dev/null 2>&1")
        os.system("pkill -f EMS.WebAPI >/dev/null 2>&1")

def main():
    # Kill any existing stale processes before launching
    kill_stale_processes()

    print("[WAIT] Du an se khoi dong sau:")
    for i in range(3, 0, -1):
        print(f"{i}...")
        time.sleep(1)

    print("\n[START] Khoi dong Web API...")
    print("\n email: admin@ems.com")
    print("\n password: AdminPassword123!")

    # Chay Web API
    api_process = subprocess.Popen(
        ["dotnet", "run", "--project", "src/EMS.WebAPI/EMS.WebAPI.csproj"],
        shell=True
    )

    # Doi vai giay de API khoi dong truoc
    time.sleep(4)

    print("[START] Khoi dong ASP.NET Core MVC (Serving Portal & Blazor WASM on port 7115)...")
    # Chay EMS.Mvc
    mvc_process = subprocess.Popen(
        ["dotnet", "run", "--project", "src/EMS.Mvc/EMS.Mvc.csproj"],
        shell=True
    )

    # Doi vai giay de cac cong khoi dong xong
    time.sleep(4)
    print("[WEB] Dang tu dong mo trinh duyet...")
    webbrowser.open("https://localhost:7115")  # Unified Portal on MVC port 7115

    try:
        print("\n[SUCCESS] Cac dich vu dang chay trong cung terminal nay. Nhan Ctrl+C de dung lai.\n")
        api_process.wait()
        mvc_process.wait()
    except KeyboardInterrupt:
        print("\n[STOP] Dang dung cac dich vu...")
        api_process.terminate()
        mvc_process.terminate()
        kill_stale_processes()
        sys.exit(0)

if __name__ == "__main__":
    main()

