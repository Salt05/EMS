import subprocess
import time
import sys
import webbrowser

# Ensure stdout supports UTF-8 for Vietnamese characters and symbols
if sys.version_info >= (3, 7):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def main():
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

    print("[START] Khoi dong Blazor WebAssembly (Admin/Organizer Portal)...")
    # Chay Blazor WASM
    wasm_process = subprocess.Popen(
        ["dotnet", "run", "--project", "src/EMS.BlazorWASM/EMS.BlazorWASM.csproj"],
        shell=True
    )

    print("[START] Khoi dong ASP.NET Core MVC (Student Portal)...")
    # Chay EMS.Mvc
    mvc_process = subprocess.Popen(
        ["dotnet", "run", "--project", "src/EMS.Mvc/EMS.Mvc.csproj"],
        shell=True
    )

    # Doi vai giay de cac cong khoi dong xong
    time.sleep(4)
    print("[WEB] Dang tu dong mo trinh duyet...")
    webbrowser.open("https://localhost:7115")  # Blazor WASM
    webbrowser.open("https://localhost:7032")  # MVC Student Portal

    try:
        print("\n[SUCCESS] Ca 3 dich vu dang chay trong cung terminal nay. Nhan Ctrl+C de dung lai.\n")
        api_process.wait()
        wasm_process.wait()
        mvc_process.wait()
    except KeyboardInterrupt:
        print("\n[STOP] Dang dung cac dich vu...")
        api_process.terminate()
        wasm_process.terminate()
        mvc_process.terminate()
        sys.exit(0)

if __name__ == "__main__":
    main()

