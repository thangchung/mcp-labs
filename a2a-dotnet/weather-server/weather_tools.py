from mcp.server.fastmcp import FastMCP
import random
from typing import Dict, Any

mcp = FastMCP("WeatherServer")

# Mock weather data for different cities
WEATHER_DATA = {
    "new york": {"temperature": 22, "humidity": 65, "condition": "partly cloudy"},
    "london": {"temperature": 15, "humidity": 78, "condition": "rainy"},
    "tokyo": {"temperature": 28, "humidity": 70, "condition": "sunny"},
    "paris": {"temperature": 18, "humidity": 72, "condition": "cloudy"},
    "sydney": {"temperature": 25, "humidity": 60, "condition": "sunny"},
    "berlin": {"temperature": 16, "humidity": 75, "condition": "overcast"},
    "moscow": {"temperature": 5, "humidity": 80, "condition": "snowy"},
    "mumbai": {"temperature": 32, "humidity": 85, "condition": "humid"}
}


@mcp.tool()
async def get_current_weather(city: str) -> Dict[str, Any]:
    """
    Get current weather information for a specific city.
    
    Args:
        city: The name of the city to get weather for
        
    Returns:
        Dictionary containing temperature (Celsius), humidity (%), and weather condition
    """
    city_lower = city.lower().strip()
    
    if city_lower in WEATHER_DATA:
        weather = WEATHER_DATA[city_lower].copy()
        # Add some randomness to make it more realistic
        weather["temperature"] += random.randint(-3, 3)
        weather["humidity"] += random.randint(-5, 5)
        weather["humidity"] = max(0, min(100, weather["humidity"]))  # Keep humidity in valid range
        
        return {
            "city": city.title(),
            "temperature": weather["temperature"],
            "humidity": weather["humidity"],
            "condition": weather["condition"],
            "units": "Celsius"
        }
    else:
        # Return a random weather condition for unknown cities
        return {
            "city": city.title(),
            "temperature": random.randint(10, 30),
            "humidity": random.randint(40, 90),
            "condition": random.choice(["sunny", "cloudy", "partly cloudy", "rainy"]),
            "units": "Celsius"
        }


@mcp.tool()
async def get_weather_forecast(city: str, days: int = 3) -> Dict[str, Any]:
    """
    Get weather forecast for a specific city for the next few days.
    
    Args:
        city: The name of the city to get forecast for
        days: Number of days to forecast (default: 3, max: 7)
        
    Returns:
        Dictionary containing forecast data for multiple days
    """
    days = min(max(1, days), 7)  # Limit to 1-7 days
    
    current_weather = await get_current_weather(city)
    base_temp = current_weather["temperature"]
    
    forecast = []
    conditions = ["sunny", "partly cloudy", "cloudy", "rainy", "overcast"]
    
    for i in range(days):
        day_temp = base_temp + random.randint(-5, 5)
        forecast.append({
            "day": i + 1,
            "temperature_high": day_temp + random.randint(2, 8),
            "temperature_low": day_temp - random.randint(2, 6),
            "humidity": random.randint(40, 90),
            "condition": random.choice(conditions)
        })
    
    return {
        "city": city.title(),
        "forecast_days": days,
        "forecast": forecast,
        "units": "Celsius"
    }


@mcp.tool()
async def convert_temperature(temperature: float, from_unit: str, to_unit: str) -> Dict[str, Any]:
    """
    Convert temperature between Celsius, Fahrenheit, and Kelvin.
    
    Args:
        temperature: The temperature value to convert
        from_unit: Source unit ('C', 'F', or 'K')
        to_unit: Target unit ('C', 'F', or 'K')
        
    Returns:
        Dictionary containing the converted temperature
    """
    from_unit = from_unit.upper()
    to_unit = to_unit.upper()
    
    # Convert to Celsius first
    if from_unit == 'F':
        celsius = (temperature - 32) * 5/9
    elif from_unit == 'K':
        celsius = temperature - 273.15
    elif from_unit == 'C':
        celsius = temperature
    else:
        raise ValueError(f"Unknown temperature unit: {from_unit}")
    
    # Convert from Celsius to target unit
    if to_unit == 'F':
        result = celsius * 9/5 + 32
    elif to_unit == 'K':
        result = celsius + 273.15
    elif to_unit == 'C':
        result = celsius
    else:
        raise ValueError(f"Unknown temperature unit: {to_unit}")
    
    return {
        "original_temperature": temperature,
        "original_unit": from_unit,
        "converted_temperature": round(result, 2),
        "converted_unit": to_unit
    }