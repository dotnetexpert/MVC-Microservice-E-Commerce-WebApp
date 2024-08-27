// Get user's geolocation coordinates
navigator.geolocation.getCurrentPosition(function (position) {
    var latitude = position.coords.latitude;
    var longitude = position.coords.longitude;
    // Send coordinates to server-side endpoint
    $.ajax({
        url: '/order/GetCountryCode',
        type: 'POST',
        data: { latitude: latitude, longitude: longitude },
        success: function (result) {
            // Handle success
        },
        error: function (xhr, status, error) {
            // Handle error
        }
    });
});