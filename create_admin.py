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

print(f"[EMS] Dang thiet lap du lieu he thong...")

# 1. Đảm bảo Tenant mặc định tồn tại
tenant_docs = db.collection("tenants").where("subdomain", "==", "default").get()
tenant_id = "default-tenant"
if len(tenant_docs) == 0:
    print("[EMS] Dang tao Default Tenant...")
    db.collection("tenants").document(tenant_id).set({
        "id": tenant_id,
        "name": "He thong Mac dinh (Local)",
        "subdomain": "default",
        "email": "contact@ems.com",
        "isActive": True,
        "createdAt": firestore.SERVER_TIMESTAMP,
        "updatedAt": firestore.SERVER_TIMESTAMP
    })
    print("[EMS] Da tao Default Tenant!")
else:
    tenant_id = tenant_docs[0].id
    print("[EMS] Default Tenant da ton tai!")

print(f"\n[EMS] Dang tao tai khoan admin: {email}...")

# 2. Tạo User trong Firebase Auth
try:
    user_record = auth.create_user(
        email=email,
        email_verified=True,
        password=password,
        display_name="System Admin",
        disabled=False)
    print(f"[EMS] Da tao Firebase Auth User UID: {user_record.uid}")
    uid = user_record.uid
except auth.EmailAlreadyExistsError:
    print(f"[EMS] Email {email} da ton tai trong Firebase Auth. Dang lay UID...")
    user_record = auth.get_user_by_email(email)
    uid = user_record.uid
except Exception as e:
    print(f"[EMS] Loi Firebase Auth: {e}")
    sys.exit(1)

# 3. Tạo/Cập nhật Document trong Firestore
docs = db.collection("users").where("email", "==", email).get()
if len(docs) > 0:
    print("[EMS] Document user da ton tai trong Firestore. Dang cap nhat quyen & tenant...")
    user_id = docs[0].id
    db.collection("users").document(user_id).update({
        "roleIds": ["superadmin", "admin", "manager"],
        "tenantId": tenant_id
    })
    print("[EMS] Da cap nhat quyen thanh cong!")
else:
    user_id = str(uuid.uuid4())
    user_doc = {
        "id": user_id,
        "firebaseUid": uid,
        "email": email,
        "fullName": "System Admin",
        "phoneNumber": "0123456789",
        "mssv": "ADMIN001",
        "department": "Ban Quan Tri",
        "tenantId": tenant_id,
        "roleIds": ["superadmin", "admin", "manager"],
        "status": 1,
        "createdAt": firestore.SERVER_TIMESTAMP,
        "updatedAt": firestore.SERVER_TIMESTAMP
    }
    db.collection("users").document(user_id).set(user_doc)
    print(f"[EMS] Da tao Document User moi trong Firestore voi ID: {user_id}")

print("\n=========================================")
print(f"TAI KHOAN ADMIN DA SAN SANG")
print(f"Email:    {email}")
print(f"Password: {password}")
print(f"Quyen:    superadmin, admin, manager")
print(f"Tenant:   {tenant_id} (subdomain: default)")
print("=========================================\n")
