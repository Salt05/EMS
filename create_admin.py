import firebase_admin
from firebase_admin import credentials
from firebase_admin import auth
from firebase_admin import firestore
import uuid
import sys

# Khởi tạo Firebase từ key
cred = credentials.Certificate("src/EMS.WebAPI/firebase-key.json")
firebase_admin.initialize_app(cred)

db = firestore.client()

email = "admin@ems.com"
password = "AdminPassword123!"

print(f"⏳ Đang thiết lập dữ liệu hệ thống...")

# 1. Đảm bảo Tenant mặc định tồn tại
tenant_docs = db.collection("tenants").where("subdomain", "==", "default").get()
tenant_id = "default-tenant"
if len(tenant_docs) == 0:
    print("⏳ Đang tạo Default Tenant...")
    db.collection("tenants").document(tenant_id).set({
        "id": tenant_id,
        "name": "Hệ thống Mặc định (Local)",
        "subdomain": "default",
        "email": "contact@ems.com",
        "isActive": True,
        "createdAt": firestore.SERVER_TIMESTAMP,
        "updatedAt": firestore.SERVER_TIMESTAMP
    })
    print("✅ Đã tạo Default Tenant!")
else:
    tenant_id = tenant_docs[0].id
    print("✅ Default Tenant đã tồn tại!")

print(f"\n⏳ Đang tạo tài khoản admin: {email}...")

# 2. Tạo User trong Firebase Auth
try:
    user_record = auth.create_user(
        email=email,
        email_verified=True,
        password=password,
        display_name="System Admin",
        disabled=False)
    print(f"✅ Đã tạo Firebase Auth User UID: {user_record.uid}")
    uid = user_record.uid
except auth.EmailAlreadyExistsError:
    print(f"⚠️ Email {email} đã tồn tại trong Firebase Auth. Đang lấy UID...")
    user_record = auth.get_user_by_email(email)
    uid = user_record.uid
except Exception as e:
    print(f"❌ Lỗi Firebase Auth: {e}")
    sys.exit(1)

# 3. Tạo/Cập nhật Document trong Firestore
docs = db.collection("users").where("email", "==", email).get()
if len(docs) > 0:
    print("⚠️ Document user đã tồn tại trong Firestore. Đang cập nhật quyền & tenant...")
    user_id = docs[0].id
    db.collection("users").document(user_id).update({
        "roleIds": ["admin", "organizer", "manager"],
        "tenantId": tenant_id
    })
    print("✅ Đã cập nhật quyền thành công!")
else:
    user_id = str(uuid.uuid4())
    user_doc = {
        "id": user_id,
        "firebaseUid": uid,
        "email": email,
        "fullName": "System Admin",
        "phoneNumber": "0123456789",
        "mssv": "ADMIN001",
        "department": "Ban Quản Trị",
        "tenantId": tenant_id,
        "roleIds": ["admin", "organizer", "manager"],
        "status": 1,
        "createdAt": firestore.SERVER_TIMESTAMP,
        "updatedAt": firestore.SERVER_TIMESTAMP
    }
    db.collection("users").document(user_id).set(user_doc)
    print(f"✅ Đã tạo Document User mới trong Firestore với ID: {user_id}")

print("\n=========================================")
print(f"🎉 TÀI KHOẢN ADMIN ĐÃ SẴN SÀNG")
print(f"👉 Email:    {email}")
print(f"👉 Password: {password}")
print(f"👉 Quyền:    admin, organizer, manager")
print(f"👉 Tenant:   {tenant_id} (subdomain: default)")
print("=========================================\n")
