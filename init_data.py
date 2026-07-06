import firebase_admin
from firebase_admin import credentials, auth, firestore
import uuid
import datetime
import random
import sys

# Ensure UTF-8 output for Windows terminal
if sys.version_info >= (3, 7):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

print("==================================================")
print("🚀  EMS SYSTEM DATA INITIALIZATION TOOL (5 UNIVERSITIES)")
print("==================================================")

# Khởi tạo Firebase
print("\n⏳ Đang kết nối Firebase...")
try:
    cred = credentials.Certificate("src/EMS.WebAPI/firebase-key.json")
    firebase_admin.initialize_app(cred)
except Exception as e:
    print(f"❌ Lỗi kết nối Firebase: {e}")
    sys.exit(1)

db = firestore.client()
now = datetime.datetime.now(datetime.timezone.utc)
COMMON_PASS = "123456"

# =========================================================
# 1. TẠO TENANTS (5 TRƯỜNG ĐẠI HỌC)
# =========================================================
print("\n🏢 [1/4] Đang khởi tạo danh sách 5 Trường Đại học (Tenants)...")
tenants_data = [
    {"id": "default-tenant", "name": "Đại học Quốc gia Hà Nội (VNU)", "subdomain": "default", "email": "contact@vnu.edu.vn"},
    {"id": "hust-tenant", "name": "Đại học Bách Khoa Hà Nội (HUST)", "subdomain": "hust", "email": "contact@hust.edu.vn"},
    {"id": "fpt-tenant", "name": "Trường Đại học FPT (FPT)", "subdomain": "fpt", "email": "contact@fpt.edu.vn"},
    {"id": "neu-tenant", "name": "Đại học Kinh tế Quốc dân (NEU)", "subdomain": "neu", "email": "contact@neu.edu.vn"},
    {"id": "rmit-tenant", "name": "Đại học RMIT Việt Nam (RMIT)", "subdomain": "rmit", "email": "contact@rmit.edu.vn"}
]

for t in tenants_data:
    db.collection("tenants").document(t["id"]).set({
        "id": t["id"],
        "name": t["name"],
        "subdomain": t["subdomain"],
        "email": t["email"],
        "isActive": True,
        "createdAt": firestore.SERVER_TIMESTAMP,
        "updatedAt": firestore.SERVER_TIMESTAMP
    })
    print(f"   + Tenant: {t['name'].ljust(35)} | Subdomain: {t['subdomain']}")

# =========================================================
# 2. TẠO USERS (FIREBASE AUTH & FIRESTORE) - 46 TÀI KHOẢN
# =========================================================
print("\n👥 [2/4] Đang khởi tạo 46 Tài khoản Users (Mật khẩu chung: 123456)...")

users_data = [
    # 1 Super Admin chung cho toàn hệ thống
    {"email": "admin@ems.com", "name": "System Super Admin", "mssv": "ADMIN001", "dept": "Ban Quản Trị Tối Cao", "roles": ["superadmin", "admin", "manager"], "tenant": "default-tenant"}
]

# Cấu hình nhân sự cho từng trường: 1 Tenant Admin, 2 Organizers, 6 Students
uni_prefixes = [
    {"code": "vnu", "tenant": "default-tenant", "name": "VNU"},
    {"code": "hust", "tenant": "hust-tenant", "name": "HUST"},
    {"code": "fpt", "tenant": "fpt-tenant", "name": "FPT"},
    {"code": "neu", "tenant": "neu-tenant", "name": "NEU"},
    {"code": "rmit", "tenant": "rmit-tenant", "name": "RMIT"},
]

for uni in uni_prefixes:
    code = uni["code"]
    tenant = uni["tenant"]
    # 1 Tenant Admin
    users_data.append({"email": f"admin.{code}@ems.com", "name": f"Admin {uni['name']}", "mssv": f"{code.upper()}ADM", "dept": f"Ban Quản Trị {uni['name']}", "roles": ["admin", "manager"], "tenant": tenant})
    # 2 Organizers
    users_data.append({"email": f"org1.{code}@ems.com", "name": f"CLB Công nghệ {uni['name']}", "mssv": f"{code.upper()}ORG1", "dept": "CLB Sự Kiện & IT", "roles": ["manager"], "tenant": tenant})
    users_data.append({"email": f"org2.{code}@ems.com", "name": f"Đoàn Hội {uni['name']}", "mssv": f"{code.upper()}ORG2", "dept": "CLB Văn hóa Nghệ thuật", "roles": ["manager"], "tenant": tenant})
    # 6 Students
    for i in range(1, 7):
        users_data.append({"email": f"sv{i}.{code}@ems.com", "name": f"Sinh viên {i} ({uni['name']})", "mssv": f"{code.upper()}{i:03d}", "dept": "Khoa Chuyên Ngành", "roles": ["student"], "tenant": tenant})

user_map = {} # email -> user_id
auth_count = 0

for u in users_data:
    email = u["email"]
    try:
        user_rec = auth.create_user(
            email=email,
            email_verified=True,
            password=COMMON_PASS,
            display_name=u["name"],
            disabled=False
        )
        uid = user_rec.uid
        auth_count += 1
    except auth.EmailAlreadyExistsError:
        user_rec = auth.get_user_by_email(email)
        uid = user_rec.uid
    except Exception as ex:
        print(f"   ⚠️ Lỗi Auth cho {email}: {ex}")
        continue

    # Create/Update Firestore User doc
    docs = list(db.collection("users").where("email", "==", email).get())
    if len(docs) > 0:
        user_id = docs[0].id
    else:
        user_id = str(uuid.uuid4())

    db.collection("users").document(user_id).set({
        "id": user_id,
        "firebaseUid": uid,
        "email": email,
        "fullName": u["name"],
        "phoneNumber": "0987654321",
        "mssv": u["mssv"],
        "department": u["dept"],
        "tenantId": u["tenant"],
        "roleIds": u["roles"],
        "status": 1,
        "createdAt": firestore.SERVER_TIMESTAMP,
        "updatedAt": firestore.SERVER_TIMESTAMP
    })
    user_map[email] = user_id

print(f"✅ Đã khởi tạo thành công {len(user_map)} tài khoản trong Firestore và Auth!")

# =========================================================
# 3. TẠO EVENTS (15 SỰ KIỆN VỚI THỜI GIAN TƯƠNG ĐỐI & IMAGE URLS)
# =========================================================
print("\n📅 [3/4] Đang khởi tạo 15 Sự kiện với hình ảnh và thời gian động...")

events_data = [
    # --- TRƯỜNG 1: VNU (default-tenant) ---
    {
        "id": "vnu-ev-01", "tenant": "default-tenant", "title": "Hội thảo Trí tuệ Nhân tạo & AI Agent 2026",
        "desc": "Cập nhật các xu hướng AI mới nhất, DeepMind Agentic Coding và ứng dụng vào thực tế.",
        "loc": "Hội trường A1 - VNU", "cap": 100, "status": 2, "org": user_map["org1.vnu@ems.com"],
        "start": now + datetime.timedelta(hours=2), "end": now + datetime.timedelta(hours=6),
        "img": "https://events.ctu.edu.vn/images/uploads/2024/2025/1.png"
    },
    {
        "id": "vnu-ev-02", "tenant": "default-tenant", "title": "Cuộc thi Lập trình VNU Hackathon",
        "desc": "Sân chơi lập trình 48h dành cho sinh viên đam mê công nghệ tại VNU.",
        "loc": "Trung tâm Đổi mới Sáng tạo VNU", "cap": 80, "status": 2, "org": user_map["org2.vnu@ems.com"],
        "start": now + datetime.timedelta(days=1, hours=2), "end": now + datetime.timedelta(days=1, hours=8),
        "img": "https://soict.hust.edu.vn/wp-content/uploads/soict-hackathon.jpg"
    },
    {
        "id": "vnu-ev-03", "tenant": "default-tenant", "title": "Workshop Viết CV & Phỏng vấn Kỹ thuật",
        "desc": "Hướng dẫn viết CV chuẩn ATS và tham gia phỏng vấn mô phỏng 1-1 với chuyên gia.",
        "loc": "Phòng họp chuyên đề 302", "cap": 10, "status": 1, "org": user_map["org1.vnu@ems.com"],
        "start": now + datetime.timedelta(days=2, hours=3), "end": now + datetime.timedelta(days=2, hours=6),
        "img": "https://mshoagiaotiep.com/uploads/images/userfiles/imgpsh_fullsize_anim_1.png"
    },

    # --- TRƯỜNG 2: HUST (hust-tenant) ---
    {
        "id": "hust-ev-01", "tenant": "hust-tenant", "title": "HUST Techday 2026: Khám phá Web3 & Blockchain",
        "desc": "Hội thảo khoa học công nghệ chuyên sâu về hợp đồng thông minh và tài chính phi tập trung.",
        "loc": "Hội trường C2 - HUST", "cap": 150, "status": 2, "org": user_map["org1.hust@ems.com"],
        "start": now + datetime.timedelta(hours=3), "end": now + datetime.timedelta(hours=7),
        "img": "https://cdn.fpt-is.com/vi/2026/02/Thumbnail-1772101607.jpg"
    },
    {
        "id": "hust-ev-02", "tenant": "hust-tenant", "title": "Đêm Nhạc Hội Bách Khoa - Chào Tân Sinh Viên",
        "desc": "Bữa tiệc âm nhạc hoành tráng chào đón thế hệ sinh viên mới Bách Khoa.",
        "loc": "Quảng trường Thư viện Tạ Quang Bửu", "cap": 300, "status": 2, "org": user_map["org2.hust@ems.com"],
        "start": now + datetime.timedelta(days=1, hours=3), "end": now + datetime.timedelta(days=1, hours=9),
        "img": "https://cdn2.tuoitre.vn/471584752817336320/2023/8/6/line-up-1691256566174871125693.png"
    },
    {
        "id": "hust-ev-03", "tenant": "hust-tenant", "title": "Giải Đấu Thể thao Điện tử Esports HUST",
        "desc": "Giải đấu thể thao điện tử bộ môn LMHT và Valorant toàn trường.",
        "loc": "Nhà thi đấu Bách Khoa", "cap": 50, "status": 2, "org": user_map["org2.hust@ems.com"],
        "start": now + datetime.timedelta(days=3, hours=1), "end": now + datetime.timedelta(days=3, hours=8),
        "img": "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ9n09mVbxyLSMgKxEol-l3DBP4ygc8ywMi-QwEP-tryD5tJn_6-_A0Y0Q&s=10"
    },

    # --- TRƯỜNG 3: FPT (fpt-tenant) ---
    {
        "id": "fpt-ev-01", "tenant": "fpt-tenant", "title": "Diễn đàn Kinh tế & Quản trị Doanh nghiệp",
        "desc": "Khám phá chiến lược chuyển đổi số và quản trị doanh nghiệp hiện đại.",
        "loc": "Hội trường Alpha - FPT Hola", "cap": 120, "status": 2, "org": user_map["org1.fpt@ems.com"],
        "start": now + datetime.timedelta(hours=1), "end": now + datetime.timedelta(hours=5),
        "img": "https://kenh14cdn.com/203336854389633024/2026/3/25/kttt-1-1774400974073-1774400974660640665038.jpg"
    },
    {
        "id": "fpt-ev-02", "tenant": "fpt-tenant", "title": "Ngày hội Tuyển dụng Career Fair FPT",
        "desc": "Kết nối trực tiếp với hơn 50 doanh nghiệp công nghệ hàng đầu.",
        "loc": "Sân trống Trống Đồng FPT", "cap": 500, "status": 2, "org": user_map["org2.fpt@ems.com"],
        "start": now + datetime.timedelta(days=1, hours=4), "end": now + datetime.timedelta(days=1, hours=10),
        "img": "https://jobs.neu.edu.vn/storage/banners/202503130924zg9h8mcl1d.png"
    },
    {
        "id": "fpt-ev-03", "tenant": "fpt-tenant", "title": "Talkshow Khởi nghiệp Đổi mới Sáng tạo",
        "desc": "Chia sẻ kinh nghiệm khởi nghiệp từ các nhà sáng lập startup công nghệ thành công.",
        "loc": "Phòng Semina Beta", "cap": 40, "status": 1, "org": user_map["org1.fpt@ems.com"],
        "start": now + datetime.timedelta(days=4, hours=2), "end": now + datetime.timedelta(days=4, hours=5),
        "img": "https://daihochoabinh.edu.vn/wp-content/uploads/2024/06/1c-17-jpeg.webp"
    },

    # --- TRƯỜNG 4: NEU (neu-tenant) ---
    {
        "id": "neu-ev-01", "tenant": "neu-tenant", "title": "Hội nghị Khoa học & Công nghệ Trẻ NEU",
        "desc": "Công bố các công trình nghiên cứu khoa học xuất sắc của sinh viên kinh tế.",
        "loc": "Hội trường A - Tòa nhà Thế kỷ NEU", "cap": 150, "status": 2, "org": user_map["org1.neu@ems.com"],
        "start": now + datetime.timedelta(hours=4), "end": now + datetime.timedelta(hours=8),
        "img": "https://ump.vnu.edu.vn/Uploads/Article/ngoclinh.ump/2026_5/images/GM%20HNKHCN%20TT%20(2).jpg"
    },
    {
        "id": "neu-ev-02", "tenant": "neu-tenant", "title": "Workshop Kỹ năng Nghiên cứu Khoa học",
        "desc": "Phương pháp luận và kỹ năng xử lý dữ liệu với SPSS và Stata.",
        "loc": "Phòng thực hành B102", "cap": 50, "status": 2, "org": user_map["org2.neu@ems.com"],
        "start": now + datetime.timedelta(days=1, hours=1), "end": now + datetime.timedelta(days=1, hours=5),
        "img": "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQNTgU1VYb035cFbEYNgO1ZSnI-lUZ6imxpsX-RHjuJzohGSRrl01mPfEA&s=10"
    },
    {
        "id": "neu-ev-03", "tenant": "neu-tenant", "title": "Giao lưu Văn hóa Sinh viên Quốc tế",
        "desc": "Đêm hội giao lưu văn hóa, ẩm thực và âm nhạc với sinh viên quốc tế.",
        "loc": "Sân ký túc xá NEU", "cap": 200, "status": 2, "org": user_map["org2.neu@ems.com"],
        "start": now + datetime.timedelta(days=5, hours=3), "end": now + datetime.timedelta(days=5, hours=7),
        "img": "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT1t2pp0S4VQ5omvijAbckeKLKONbunJNgIzJKMTpqObE0iMoMFHKNTA_d-&s=10"
    },

    # --- TRƯỜNG 5: RMIT (rmit-tenant) ---
    {
        "id": "rmit-ev-01", "tenant": "rmit-tenant", "title": "RMIT Innovation & Leadership Summit 2026",
        "desc": "Hội nghị đỉnh cao về đổi mới sáng tạo và tư duy lãnh đạo trẻ trong kỷ nguyên số.",
        "loc": "RMIT Melbourne Hall", "cap": 100, "status": 2, "org": user_map["org1.rmit@ems.com"],
        "start": now + datetime.timedelta(hours=2), "end": now + datetime.timedelta(hours=6),
        "img": "https://i.ytimg.com/vi/mHTSA09fWSU/maxresdefault.jpg"
    },
    {
        "id": "rmit-ev-02", "tenant": "rmit-tenant", "title": "Digital Marketing & Advertising Workshop",
        "desc": "Trải nghiệm thực chiến xây dựng chiến lược truyền thông thương hiệu toàn diện.",
        "loc": "Creative Media Lab 2", "cap": 60, "status": 2, "org": user_map["org2.rmit@ems.com"],
        "start": now + datetime.timedelta(days=1, hours=5), "end": now + datetime.timedelta(days=1, hours=9),
        "img": "https://digitalmarketacademy.in/wp-content/uploads/2025/09/Digital-Marketing-Workshop-2026_-Hands-On-Learning-Experience.jpg"
    },
    {
        "id": "rmit-ev-03", "tenant": "rmit-tenant", "title": "Triển lãm Nghệ thuật & Thiết kế Truyền thông",
        "desc": "Không gian trưng bày các đồ án tốt nghiệp xuất sắc ngành Thiết kế Sáng tạo.",
        "loc": "RMIT Art Gallery", "cap": 150, "status": 1, "org": user_map["org2.rmit@ems.com"],
        "start": now + datetime.timedelta(days=6, hours=2), "end": now + datetime.timedelta(days=6, hours=8),
        "img": "https://cdn.advertisingvietnam.com/image/2025/11/05/1762333749331.png"
    }
]

for e in events_data:
    db.collection("events").document(e["id"]).set({
        "id": e["id"],
        "tenantId": e["tenant"],
        "title": e["title"],
        "description": e["desc"],
        "location": e["loc"],
        "capacity": e["cap"],
        "organizerId": e["org"],
        "status": e["status"],
        "startTime": e["start"],
        "endTime": e["end"],
        "imageUrl": e["img"],
        "createdAt": firestore.SERVER_TIMESTAMP,
        "updatedAt": firestore.SERVER_TIMESTAMP
    })
    status_str = "Approved" if e["status"] == 2 else "Pending"
    print(f"   + Event: {e['title'][:38].ljust(38)} | Tenant: {e['tenant'].ljust(14)} | Start: {e['start'].strftime('%Y-%m-%d %H:%M')}")

# =========================================================
# 4. TẠO REGISTRATIONS & CHECK-INS (NGẪU NHIÊN CHO TỪNG TRƯỜNG)
# =========================================================
print("\n📝 [4/4] Đang khởi tạo danh sách Đăng ký & Điểm danh ngẫu nhiên...")

total_regs = 0
reg_id_counter = 1

# Lặp qua từng trường để sinh ngẫu nhiên lượt đăng ký của sinh viên trường đó vào sự kiện trường đó
for uni in uni_prefixes:
    code = uni["code"]
    tenant = uni["tenant"]
    
    uni_students = [user_map[f"sv{i}.{code}@ems.com"] for i in range(1, 7)]
    uni_events = [e["id"] for e in events_data if e["tenant"] == tenant]
    
    # Cho mỗi sự kiện của trường, tạo ngẫu nhiên từ 3 đến 5 sinh viên đăng ký
    for ev_id in uni_events:
        selected_students = random.sample(uni_students, k=random.randint(3, 5))
        for student_id in selected_students:
            reg_id = f"reg-{code}-{reg_id_counter:03d}"
            reg_id_counter += 1
            
            # Phân bố trạng thái ngẫu nhiên: 50% Approved & CheckedIn, 25% Approved chưa checkin, 15% Pending, 10% Waitlist/Reject
            rand_val = random.random()
            if rand_val < 0.50:
                status = 2 # Approved
                checked_in = True
                code_str = f"{code.upper()}-{reg_id_counter:03d}"
                note = "Em đăng ký tham gia đầy đủ ạ."
                reason = ""
            elif rand_val < 0.75:
                status = 2 # Approved
                checked_in = False
                code_str = f"{code.upper()}-{reg_id_counter:03d}"
                note = "Đã nhận được email xác nhận."
                reason = ""
            elif rand_val < 0.90:
                status = 1 # Pending
                checked_in = False
                code_str = ""
                note = "Em vừa gửi đơn đăng ký."
                reason = ""
            else:
                status = random.choice([3, 4]) # Waitlist or Rejected
                checked_in = False
                code_str = ""
                note = "Đăng ký bổ sung."
                reason = "Sự kiện đã hết số lượng đăng ký ưu tiên." if status == 4 else ""
                
            reg_time = now - datetime.timedelta(hours=random.randint(5, 48))
            check_time = now - datetime.timedelta(minutes=random.randint(10, 180)) if checked_in else None
            
            db.collection("registrations").document(reg_id).set({
                "id": reg_id,
                "tenantId": tenant,
                "eventId": ev_id,
                "userId": student_id,
                "note": note,
                "status": status,
                "registeredAt": reg_time,
                "processedById": user_map[f"admin.{code}@ems.com"] if status > 1 else "",
                "rejectionReason": reason,
                "checkInCode": code_str,
                "checkedIn": checked_in,
                "checkedInAt": check_time,
                "reminderSent": False,
                "createdAt": firestore.SERVER_TIMESTAMP,
                "updatedAt": firestore.SERVER_TIMESTAMP
            })
            total_regs += 1

print(f"✅ Đã tạo thành công {total_regs} lượt Đăng ký & Điểm danh ngẫu nhiên trên toàn bộ 5 trường!")

print("\n==================================================")
print("🎉 KHOI TAO DU LIEU TONGS QUAN THANH CONG!")
print("==================================================")
print("👉 Mật khẩu chung cho TOÀN BỘ 46 tài khoản: 123456")
print("👉 Tài khoản Super Admin: admin@ems.com")
print("👉 5 Tenant Admins: admin.vnu@ems.com, admin.hust@ems.com, admin.fpt@ems.com, admin.neu@ems.com, admin.rmit@ems.com")
print("==================================================\n")
