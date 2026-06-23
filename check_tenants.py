import firebase_admin
from firebase_admin import credentials, firestore

cred = credentials.Certificate("src/EMS.WebAPI/firebase-key.json")
firebase_admin.initialize_app(cred)
db = firestore.client()

docs = db.collection("tenants").get()
print(f"Total tenants in Firestore: {len(docs)}")
for doc in docs:
    print(f"- ID: {doc.id}, Name: {doc.to_dict().get('name')}")
