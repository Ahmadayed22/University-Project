/*******************************
   SIGNUP LOGIC (for signup.html)
*******************************/
signupForm.addEventListener('submit', async function (event) {
    event.preventDefault(); // Prevent default form submission

    // Disable the submit button to prevent multiple clicks
    document.querySelector("button[type=submit]").disabled = true;

    // Gather user input
    const insName = document.getElementById('signupInsName').value.trim();
    const insCountry = document.getElementById('signupInsCountry').value.trim();
    const email = document.getElementById('signupEmail').value.trim();
    const password = document.getElementById('signupPassword').value;
    const specialityValues = Array.from(document.querySelectorAll('.speciality-checkbox:checked')).map(input => input.value);

    // Validate input
    if (!insName || !insCountry || !email || !password || specialityValues.length === 0) {
        showSignupError("All fields are required.");
        document.querySelector("button[type=submit]").disabled = false; // Re-enable button
        return;
    }

    // Prepare user data
    const userPayload = {
        insName,
        insCountry,
        email,
        password,
        verificationCode: "abc123",
        verified: 0,
        clearance: 0,
        speciality: specialityValues
    };

    try {
        const response = await fetch('/api/users/signup', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(userPayload)
        });

        if (!response.ok) {
            const errData = await response.json();
            showSignupError(errData.message || "Sign-up failed.");
            document.querySelector("button[type=submit]").disabled = false; // Re-enable button
            return;
        }

        const data = await response.json();
        if (!data.success) {
            showSignupError(data.message || "Sign-up failed.");
            document.querySelector("button[type=submit]").disabled = false;
            return;
        }

        alert("Sign-up successful! Redirecting to login...");
        window.location.href = "/login.html";
    } catch (err) {
        console.error("Sign-up error:", err);
        showSignupError("Server error. Try again.");
        document.querySelector("button[type=submit]").disabled = false;
    }
    function showSignupError(msg) {
        const errDiv = document.getElementById('signupError');
        errDiv.style.display = 'block';
        errDiv.textContent = msg;
    }

});



