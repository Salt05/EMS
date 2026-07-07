import firebase_admin
from firebase_admin import credentials
from firebase_admin import firestore
from datetime import datetime, timedelta, timezone
import sys
import os

# Set environment for Firestore Emulator if running locally
# os.environ["FIRESTORE_EMULATOR_HOST"] = "localhost:8080"

print("⏳ Khởi tạo kết nối Firestore...")
try:
    # Thử khởi tạo với service account key nếu có
    key_path = "src/EMS.WebAPI/firebase-key.json"
    if os.path.exists(key_path):
        cred = credentials.Certificate(key_path)
        firebase_admin.initialize_app(cred)
    else:
        # Nếu chạy emulator, có thể khởi tạo mặc định
        firebase_admin.initialize_app()
except Exception as e:
    print(f"⚠️ Không thể khởi tạo Firebase mặc định: {e}")
    print("Thử khởi tạo bằng dự án demo...")
    # Fallback cho emulator
    cred = credentials.AnonymousCredentials()
    firebase_admin.initialize_app(cred, {'projectId': 'demo-project'})

db = firestore.client()

tenant_id = "default-tenant"
# Kiểm tra xem tenant có tồn tại không, nếu không lấy tenant đầu tiên
tenants = db.collection("tenants").limit(1).get()
if len(tenants) > 0:
    tenant_id = tenants[0].id
    print(f"✅ Sử dụng Tenant ID hiện có: {tenant_id}")
else:
    print(f"⚠️ Không tìm thấy tenant nào, sử dụng mặc định: {tenant_id}")

now = datetime.now(timezone.utc)

# 1. Tạo sự kiện Đang diễn ra (evt-checkin-demo)
event_demo_id = "evt-checkin-demo"
event_demo_data = {
    "id": event_demo_id,
    "tenantId": tenant_id,
    "title": "Demo Check-in: Hội thảo Kỹ năng mềm",
    "description": "Sự kiện đang diễn ra ngay bây giờ. Dùng mã ABC123 để test tính năng check-in.",
    "location": "Phòng Hội thảo C301",
    "startTime": now - timedelta(minutes=30),
    "endTime": now + timedelta(hours=3),
    "capacity": 50,
    "imageUrl": "https://images.unsplash.com/photo-1552664730-d307ca884978?w=800&auto=format&fit=crop&q=60",
    "organizerId": "admin-user",
    "status": 2,  # Approved
    "approvedById": "admin-user",
    "approvedAt": now - timedelta(days=1),
    "checkInCode": "ABC123",
    "checkInCodeExpiredAt": now + timedelta(hours=3),
    "createdAt": now - timedelta(days=2),
    "updatedAt": now - timedelta(days=1)
}

# 2. Tạo sự kiện Mã hết hạn (evt-checkin-expired)
event_expired_id = "evt-checkin-expired"
event_expired_data = {
    "id": event_expired_id,
    "tenantId": tenant_id,
    "title": "Demo Check-in: [Hết Hạn] Seminar Hướng nghiệp",
    "description": "Sự kiện đang diễn ra nhưng mã check-in đã hết hạn. Dùng mã EXP999 để test.",
    "location": "Hội trường A",
    "startTime": now - timedelta(hours=1),
    "endTime": now + timedelta(hours=2),
    "capacity": 100,
    "imageUrl": "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=800&auto=format&fit=crop&q=60",
    "organizerId": "admin-user",
    "status": 2,  # Approved
    "approvedById": "admin-user",
    "approvedAt": now - timedelta(days=1),
    "checkInCode": "EXP999",
    "checkInCodeExpiredAt": now - timedelta(minutes=5),  # Hết hạn 5 phút trước
    "createdAt": now - timedelta(days=2),
    "updatedAt": now - timedelta(days=1)
}

print(f"⏳ Đang seed event {event_demo_id}...")
db.collection("events").document(event_demo_id).set(event_demo_data)

print(f"⏳ Đang seed event {event_expired_id}...")
db.collection("events").document(event_expired_id).set(event_expired_data)

# Đăng ký sẵn cho admin@ems.com (nếu tài khoản này tồn tại)
# Tìm user email admin@ems.com
users = db.collection("users").where("email", "==", "admin@ems.com").limit(1).get()
if len(users) > 0:
    user_data = users[0].to_dict()
    student_name = user_data.get("fullName", "System Admin")
    
    # Tạo đăng ký mẫu cho cả 2 sự kiện
    for eid in [event_demo_id, event_expired_id]:
        reg_id = f"reg-{eid}-admin"
        reg_data = {
            "id": reg_id,
            "tenantId": tenant_id,
            "eventId": eid,
            "studentEmail": "admin@ems.com",
            "studentName": student_name,
            "status": 1,  # Approved
            "checkedIn": False,
            "createdAt": now,
            "updatedAt": now
        }
        db.collection("registrations").document(reg_id).set(reg_data)
    print("✅ Đã tự động tạo Đăng ký mẫu ở trạng thái Approved cho tài khoản admin@ems.com!")

print("\n=========================================")
# Print success messages
print("🎉 ĐÃ SEED DỮ LIỆU SỰ KIỆN TEST CHECK-IN THÀNH CÔNG VÀO FIRESTORE!")
print(f"1. {event_demo_data['title']} (Mã check-in: ABC123)")
print(f"2. {event_expired_data['title']} (Mã check-in: EXP999 - Đã hết hạn)")
print("=========================================\n")
