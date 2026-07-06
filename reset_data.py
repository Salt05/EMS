import firebase_admin
from firebase_admin import credentials, auth, firestore
import sys
import time

# Ensure UTF-8 output for Windows terminal
if sys.version_info >= (3, 7):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

print("==================================================")
print("🗑️  EMS SYSTEM DATA RESET TOOL")
print("==================================================")

# Allow non-interactive execution with --force or -y
force = "--force" in sys.argv or "-y" in sys.argv
if not force:
    confirm = input("⚠️  CẢNH BÁO: Hành động này sẽ XÓA TOÀN BỘ dữ liệu trên Firestore và Firebase Auth!\nBạn có chắc chắn muốn tiếp tục không? (y/N): ")
    if confirm.lower().strip() != 'y':
        print("❌ Đã hủy thao tác reset dữ liệu.")
        sys.exit(0)

# Khởi tạo Firebase
print("\n⏳ Đang kết nối Firebase...")
try:
    cred = credentials.Certificate("src/EMS.WebAPI/firebase-key.json")
    firebase_admin.initialize_app(cred)
except Exception as e:
    print(f"❌ Lỗi kết nối Firebase: {e}")
    sys.exit(1)

db = firestore.client()

def delete_collection(coll_ref, batch_size=50):
    docs = list(coll_ref.limit(batch_size).stream())
    deleted = 0
    for doc in docs:
        print(f"   - Xóa doc [{coll_ref.id}]: {doc.id}")
        doc.reference.delete()
        deleted += 1
    if deleted >= batch_size:
        delete_collection(coll_ref, batch_size)

# 1. Xóa toàn bộ collections trong Firestore
print("\n🔥 [1/2] Đang xóa toàn bộ dữ liệu trên Firestore...")
collections = list(db.collections())
coll_count = 0
for coll in collections:
    print(f"\n📂 Đang dọn dẹp collection: '{coll.id}'...")
    delete_collection(coll)
    coll_count += 1

if coll_count == 0:
    print("ℹ️  Firestore hiện đang trống, không có collection nào để xóa.")
else:
    print(f"\n✅ Đã xóa sạch {coll_count} collections trên Firestore!")

# 2. Xóa toàn bộ user trong Firebase Auth
print("\n🔥 [2/2] Đang xóa toàn bộ tài khoản trên Firebase Auth...")
page = auth.list_users()
auth_count = 0
while page:
    for user in page.users:
        print(f"   - Xóa Auth User: {user.email or user.uid} ({user.uid})")
        try:
            auth.delete_user(user.uid)
            auth_count += 1
        except Exception as e:
            print(f"     ⚠️ Lỗi khi xóa {user.uid}: {e}")
    page = page.get_next_page()

print(f"\n✅ Đã xóa sạch {auth_count} tài khoản trên Firebase Auth!")
print("\n==================================================")
print("✨ HỆ THỐNG ĐÃ ĐƯỢC RESET VỀ TRẠNG THÁI TRỐNG!")
print("==================================================\n")
