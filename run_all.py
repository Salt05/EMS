import subprocess
import time
import sys
import webbrowser

def main():
    print("⏳ Dự án sẽ khởi động sau:")
    for i in range(3, 0, -1):
        print(f"{i}...")
        time.sleep(1)

    print("\n🚀 Khởi động Web API...")
    print("\n email: admin@ems.com")
    print("\n password: AdminPassword123!")

    # Chạy Web API
    api_process = subprocess.Popen(
        ["dotnet", "run", "--project", "src/EMS.WebAPI/EMS.WebAPI.csproj"],
        shell=True
    )

    # Đợi vài giây để API khởi động trước
    time.sleep(4)

    print("🚀 Khởi động Blazor WebAssembly...")
    # Chạy Blazor WASM
    wasm_process = subprocess.Popen(
        ["dotnet", "run", "--project", "src/EMS.BlazorWASM/EMS.BlazorWASM.csproj"],
        shell=True
    )

    # Đợi vài giây để Blazor khởi động xong
    time.sleep(4)
    print("🌐 Đang tự động mở trình duyệt...")
    webbrowser.open("https://localhost:7115")

    try:
        print("\n✅ Cả 2 dịch vụ đang chạy trong cùng terminal này. Nhấn Ctrl+C để dừng lại.\n")
        api_process.wait()
        wasm_process.wait()
    except KeyboardInterrupt:
        print("\n🛑 Đang dừng các dịch vụ...")
        api_process.terminate()
        wasm_process.terminate()
        sys.exit(0)

if __name__ == "__main__":
    main()
