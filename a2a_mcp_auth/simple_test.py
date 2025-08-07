"""Simple manual test script to verify ping-pong functionality."""

import asyncio
import httpx

async def test_manual_ping():
    """Test the manual ping endpoint on the ping service."""
    print("🏓 Testing Test Ping Endpoint...\n")
    
    async with httpx.AsyncClient() as client:
        try:
            print("📤 Sending test ping request...")
            response = await client.post(
                "http://localhost:8000/ping/test",
                headers={"Content-Type": "application/json"},
                timeout=10.0
            )
            
            if response.status_code == 200:
                result = response.json()
                print(f"📥 Response: {result}")
                if result.get("status") == "success" and "pong" in str(result).lower():
                    print("✅ Test ping successful - received pong response!")
                else:
                    print("⚠️  Unexpected response format")
            else:
                print(f"❌ Request failed with status {response.status_code}")
                print(f"   Response: {response.text}")
                
        except Exception as e:
            print(f"❌ Error: {str(e)}")

async def test_direct_pong():
    """Test the direct pong endpoint."""
    print("\n🏓 Testing Direct Pong Endpoint...\n")
    
    async with httpx.AsyncClient() as client:
        try:
            print("📤 Sending direct test pong request...")
            response = await client.post(
                "http://localhost:8001/pong/test",
                json={"message": "ping"},
                headers={"Content-Type": "application/json"},
                timeout=10.0
            )
            
            if response.status_code == 200:
                result = response.json()
                print(f"📥 Response: {result}")
                if result.get("status") == "success" and result.get("message") == "pong":
                    print("✅ Direct pong test successful!")
                else:
                    print("⚠️  Unexpected response format")
            else:
                print(f"❌ Request failed with status {response.status_code}")
                print(f"   Response: {response.text}")
                
        except Exception as e:
            print(f"❌ Error: {str(e)}")

async def test_service_health():
    """Test health endpoints."""
    print("\n🏥 Testing Health Endpoints...\n")
    
    services = [
        ("Ping Service", "http://localhost:8000/health"),
        ("Pong Service", "http://localhost:8001/health")
    ]
    
    async with httpx.AsyncClient() as client:
        for name, url in services:
            try:
                response = await client.get(url)
                if response.status_code == 200:
                    result = response.json()
                    print(f"✅ {name}: {result}")
                else:
                    print(f"❌ {name}: Status {response.status_code}")
            except Exception as e:
                print(f"❌ {name}: Error - {str(e)}")

async def main():
    await test_service_health()
    await test_direct_pong()
    await test_manual_ping()

if __name__ == "__main__":
    asyncio.run(main())
