import firebase_admin
from firebase_admin import credentials
from firebase_admin import auth
from firebase_admin import firestore
import uuid
import datetime
import sys

# 1. Initialize Firebase
print("⏳ Connecting to Firebase...")
try:
    cred = credentials.Certificate("src/EMS.WebAPI/firebase-key.json")
    firebase_admin.initialize_app(cred)
except Exception as e:
    print(f"❌ Error initializing Firebase: {e}")
    sys.exit(1)

db = firestore.client()
tenant_id = "default-tenant"

# 2. Get Admin user as Organizer
print("⏳ Finding admin account for organizerId...")
admin_docs = db.collection("users").where("email", "==", "admin@ems.com").get()
if len(admin_docs) > 0:
    organizer_id = admin_docs[0].id
    print(f"✅ Found admin ID: {organizer_id}")
else:
    organizer_id = str(uuid.uuid4())
    print(f"⚠️ Admin not found, generating temporary ID: {organizer_id}")

# 3. Create a Test Event with Capacity = 2
event_id = "test-event-001"
print(f"⏳ Creating test event '{event_id}' with Capacity=2...")

now = datetime.datetime.now(datetime.timezone.utc)
start_time = now + datetime.timedelta(days=1)
end_time = start_time + datetime.timedelta(hours=2)

event_doc = {
    "id": event_id,
    "tenantId": tenant_id,
    "title": "Sự kiện Hội thảo Công nghệ (Test)",
    "description": "Sự kiện được tạo tự động để kiểm tra tính năng Duyệt đăng ký & Waitlist.",
    "location": "Hội trường A1 - Tầng 2",
    "capacity": 2,
    "organizerId": organizer_id,
    "status": 2,  # Approved
    "createdAt": firestore.SERVER_TIMESTAMP,
    "updatedAt": firestore.SERVER_TIMESTAMP,
    "startTime": start_time,
    "endTime": end_time
}

db.collection("events").document(event_id).set(event_doc)
print("✅ Test event created successfully!")

# 4. Create 3 Test Students
students_data = [
    {"email": "studentA@ems.com", "name": "Nguyễn Văn A", "mssv": "SV001"},
    {"email": "studentB@ems.com", "name": "Trần Thị B", "mssv": "SV002"},
    {"email": "studentC@ems.com", "name": "Lê Văn C", "mssv": "SV003"}
]

student_ids = {}

for s in students_data:
    email = s["email"]
    print(f"\n⏳ Creating student account: {email}...")
    
    # 4.1 Firebase Auth
    try:
        user_rec = auth.create_user(
            email=email,
            email_verified=True,
            password="StudentPassword123!",
            display_name=s["name"]
        )
        uid = user_rec.uid
        print(f"✅ Created Firebase Auth User UID: {uid}")
    except auth.EmailAlreadyExistsError:
        user_rec = auth.get_user_by_email(email)
        uid = user_rec.uid
        print(f"⚠️ User already exists in Auth, UID: {uid}")
    except Exception as ex:
        print(f"❌ Firebase Auth error for {email}: {ex}")
        continue

    # 4.2 Firestore User Document
    user_docs = db.collection("users").where("email", "==", email).get()
    if len(user_docs) > 0:
        student_id = user_docs[0].id
        db.collection("users").document(student_id).update({
            "fullName": s["name"],
            "mssv": s["mssv"],
            "tenantId": tenant_id
        })
        print(f"✅ Updated existing Firestore User ID: {student_id}")
    else:
        student_id = str(uuid.uuid4())
        user_doc = {
            "id": student_id,
            "firebaseUid": uid,
            "email": email,
            "fullName": s["name"],
            "mssv": s["mssv"],
            "phoneNumber": "0987654321",
            "department": "Khoa CNTT",
            "tenantId": tenant_id,
            "roleIds": ["student"],
            "status": 1,
            "createdAt": firestore.SERVER_TIMESTAMP,
            "updatedAt": firestore.SERVER_TIMESTAMP
        }
        db.collection("users").document(student_id).set(user_doc)
        print(f"✅ Created new Firestore User ID: {student_id}")
        
    student_ids[email] = student_id

# 5. Create 3 registrations with correct status and timestamps
print("\n⏳ Creating registrations...")

# Registration A (Pending)
reg_a_id = "reg-student-a"
db.collection("registrations").document(reg_a_id).set({
    "id": reg_a_id,
    "tenantId": tenant_id,
    "eventId": event_id,
    "userId": student_ids["studentA@ems.com"],
    "note": "Em đăng ký tham gia sớm.",
    "status": 1,  # Pending
    "registeredAt": now - datetime.timedelta(minutes=10),
    "processedById": "",
    "rejectionReason": "",
    "checkInCode": "",
    "checkedIn": False,
    "reminderSent": False,
    "createdAt": firestore.SERVER_TIMESTAMP,
    "updatedAt": firestore.SERVER_TIMESTAMP
})
print("✅ Created registration for Student A (Pending)")

# Registration B (Pending)
reg_b_id = "reg-student-b"
db.collection("registrations").document(reg_b_id).set({
    "id": reg_b_id,
    "tenantId": tenant_id,
    "eventId": event_id,
    "userId": student_ids["studentB@ems.com"],
    "note": "Đăng ký nhóm nghiên cứu khoa học.",
    "status": 1,  # Pending
    "registeredAt": now - datetime.timedelta(minutes=5),
    "processedById": "",
    "rejectionReason": "",
    "checkInCode": "",
    "checkedIn": False,
    "reminderSent": False,
    "createdAt": firestore.SERVER_TIMESTAMP,
    "updatedAt": firestore.SERVER_TIMESTAMP
})
print("✅ Created registration for Student B (Pending)")

# Registration C (Waitlisted)
reg_c_id = "reg-student-c"
db.collection("registrations").document(reg_c_id).set({
    "id": reg_c_id,
    "tenantId": tenant_id,
    "eventId": event_id,
    "userId": student_ids["studentC@ems.com"],
    "note": "Em đăng ký dự khuyết.",
    "status": 3,  # Waitlisted
    "registeredAt": now,
    "processedById": "",
    "rejectionReason": "",
    "checkInCode": "",
    "checkedIn": False,
    "reminderSent": False,
    "createdAt": firestore.SERVER_TIMESTAMP,
    "updatedAt": firestore.SERVER_TIMESTAMP
})
print("✅ Created registration for Student C (Waitlisted)")

print("\n=======================================================")
print("🎉 THIẾT LẬP DỮ LIỆU TEST THÀNH CÔNG!")
print(f"👉 Event ID: {event_id} (Title: Sự kiện Hội thảo Công nghệ (Test))")
print("👉 Student A: Nguyễn Văn A (Trạng thái: Pending)")
print("👉 Student B: Trần Thị B (Trạng thái: Pending)")
print("👉 Student C: Lê Văn C (Trạng thái: Waitlisted)")
print("=======================================================\n")
