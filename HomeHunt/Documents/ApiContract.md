# Unified API Contracts

## Search API with Filters and Pagination

### Description
Allows users to search for properties based on filters such as city, price range, bedrooms, status, and rent duration, with pagination support.

### HTTP Method
`GET`

### URL
`/api/properties/search`

### Request
#### Parameters (FiltersDTO with Pagination)
| Parameter     | Type   | Description                                      |
|---------------|--------|--------------------------------------------------|
| `City`        | string | Filter by city name (e.g., "New York").          |
| `MinPrice`    | float  | Minimum price (e.g., 50000).                     |
| `MaxPrice`    | float  | Maximum price (e.g., 200000).                    |
| `Bedrooms`    | int    | Number of bedrooms (e.g., 2).                    |
| `Status`      | string | Filter by status ("For Rent" or "For Sale").     |
| `RentDuration`| string | Rent duration type ("Monthly" or "Annual").      |
| `PageNumber`  | int    | The page number for pagination (e.g., 1).        |
| `PageSize`    | int    | The number of items per page (e.g., 10).         |

#### Sample Request
```
GET /api/properties /search?City=New York&MinPrice=50000&MaxPrice=200000&Bedrooms=2&Status=For Rent&RentDuration=Monthly&PageNumber=1&PageSize=10
```


### Response
#### Success Response (200 OK)
Returns a paginated list of properties matching the filters.
```json
{
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalItems": 50,
    "startIndex": 1,
    "endIndex": 10,
    "properties": [
        {
            "propertyId": 1,
            "city": "New York",
            "price": 150000,
            "status": "For Rent",
            "rentDuration": "Monthly",
            "bedrooms": 2,
            "description": "Cozy 2-bedroom apartment in the heart of New York."
        },
        {
            "propertyId": 2,
            "city": "New York",
            "price": 180000,
            "status": "For Rent",
            "rentDuration": "Monthly",
            "bedrooms": 3,
            "description": "Spacious 3-bedroom apartment with amazing views."
        }
    ]
}
```

Error Responses
| Status Code | Message                | Description                                |
|-------------|------------------------|--------------------------------------------|
| 400         | "Invalid price range"  | MinPrice is greater than MaxPrice.         |
| 404         | "No properties found"  | No properties match the search criteria.   |
| 500         | "Unexpected error"     | Something went wrong on the server.        |


### Sample Error Response

#### 400 Bad Request
```json
{
    "error": "Invalid price range. Ensure that MinPrice is less than MaxPrice."
}
```

#### 404 Not Found
```json
{
    "error": "No properties found matching the given criteria."
}
```

#### 500 Internal Server Error
```json
{
    "error": "Unexpected error occurred. Please try again later."
}
```

## Sign Up API
### Description
Creates a new user account with provided personal details and credentials.

### HTTP Method
`POST`

### URL
`/api/users/signup`

### Request

#### Headers
- `Content-Type: application/json`

#### Body 
| Field         | Type    | Description                                                        |
|---------------|---------|--------------------------------------------------------------------|
| firstName     | string  | Required, min 3 characters, max 20 characters.                     |
| lastName      | string  | Required, min 3 characters, max 20 characters.                     |
| password      | string  | Required, min 8 characters, must include uppercase, lowercase, and number. |
| confirmPassword | string | Required, must match password.                                    |
| email         | string  | Required, valid email format, max 256 characters.                  |
| mobileNumber  | string  | Optional, max 30 characters, digits/dashes/plus sign only.         |


### Sample Request
``` json
{
    "firstName": "John",
    "lastName": "Doe",
    "password": "Password123",
    "confirmPassword": "Password123",
    "email": "john.doe@example.com",
    "mobileNumber": "+1-123-456-7890"
}
```


### Response
#### Success Response (201 Created)
Returns the unique ID of the newly created user.

```json
{
    "userId": 123
}
```

### Error Responses

| Status Code | Message                          | Description                                |
|-------------|----------------------------------|--------------------------------------------|
| 400         | Validation error or email already exists | Invalid input or email is taken.           |
| 500         | "An unexpected error occurred"   | Something went wrong on the server.        |


### Sample Error Response

- #### 400 Bad Request
```json
{
    "email": ["Invalid email address format."]
}
```

- #### 500 Internal Server Error
```json
{
    "error": "An unexpected error occurred. Please try again later."
}
```


## Top 5 Cities API
### Description
Returns a list of the top 5 cities with the most properties, including city name, property count, and an image URL.

### HTTP Method
`GET`

### URL
`/api/top-cities`

### Request
#### Parameters
- None required.

#### Sample Request

``` json
GET /api/top-cities
```


### Response
#### Success Response (200 OK)
Returns a list of the top 5 cities with the most properties.
```json
{
    "cities": [
        {
            "city": "New York",
            "numberOfProperties": 120,
            "imageUrl": "https://example.com/images/new-york.jpg"
        },
        {
            "city": "Los Angeles",
            "numberOfProperties": 98,
            "imageUrl": "https://example.com/images/los-angeles.jpg"
        },
        {
            "city": "San Francisco",
            "numberOfProperties": 80,
            "imageUrl": "https://example.com/images/san-francisco.jpg"
        },
        {
            "city": "Miami",
            "numberOfProperties": 75,
            "imageUrl": "https://example.com/images/miami.jpg"
        },
        {
            "city": "Chicago",
            "numberOfProperties": 65,
            "imageUrl": "https://example.com/images/chicago.jpg"
        }
    ]
}
```


#### Error Responses
| Status Code | Message                | Description                                |
|-------------|------------------------|--------------------------------------------|
| 500         | "Unexpected error"     | Something went wrong on the server.        |


### Sample Error Response
- 500 Internal Server Error
```json
{
    "error": "Unexpected error occurred. Please try again later."
}
```

## Login API

### Description
Authenticates a user by verifying their email and password, returning a JSON Web Token (JWT) upon successful authentication.

### HTTP Method
`POST`

### URL
`/api/users/login`

### Request
#### Headers
- `Content-Type: application/json`

#### Body
| Field         | Type    | Description                                                        |
|---------------|---------|--------------------------------------------------------------------|
| email         | string  | Required, valid email format, max 256 characters.                  |
| password      | string  | Required, min 8 characters, must include uppercase, lowercase, and number. |

#### Sample Request
``` json
POST /api/users/login HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{
    "email": "alex@example.com",
    "password": "password123"
}
```

### Response
#### Success Response (200 OK)
Returns a JSON Web Token (JWT) for the authenticated user.
```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkFsZXgiLCJpYXQiOjE1MTYyMzkwMjJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
}
```


#### Error Responses
| Status Code | Message                | Description                                |
|-------------|------------------------|--------------------------------------------|
| 400         | "Invalid or missing request data" | Email or password is null or empty.        |
| 401         | "Authentication failed" | Incorrect credentials.                     |



### Sample Error Response
- 400 Bad Request
```json
{
    "error": "Email and password are required."
}
```

- 401 Unauthorized
```json
{
    "error": "Invalid email or password."
}
```

