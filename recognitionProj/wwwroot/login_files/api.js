document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');
    if (!loginForm) return;

    loginForm.addEventListener('submit', async function (event) {
        event.preventDefault();

        const email = document.getElementById('email').value.trim();
        const password = document.getElementById('password').value;
        const verificationCode = document.getElementById('verificationCode').value.trim();
        const applicationType = document.getElementById('applicationType').value;
        // Get the Acceptance Record ID if present.
        const acceptanceRecordID = document.getElementById('recordId').value.trim();

        if (!email || !password || !verificationCode) {
            showError("Email, password, and verification code cannot be empty.");
            return;
        }

        // Build the payload. Only include the acceptanceRecordID if the application is nonconventional.
        const loginPayload = {
            email: email,
            password: password,
            verificationCode: verificationCode,
            ApplicationType: applicationType,
            AcceptanceRecordNumber: (applicationType === "nonconventional") ? acceptanceRecordID : ""
        };


        try {
            const response = await fetch('/api/users/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(loginPayload)
            });

            if (!response.ok) {
                const errorData = await response.json();
                showError(errorData.message || "Login failed.");
                return;
            }

            const data = await response.json();
            if (!data.success) {
                showError(data.message || "Login failed.");
                return;
            }

            console.log("Login success:", data);

            // Save user data in localStorage.
            localStorage.setItem('UserEmail', email);
            localStorage.setItem('InsID', data.insID);
            localStorage.setItem('InsName', data.insName);
            localStorage.setItem('Clearance', data.clearance);
            localStorage.setItem('Speciality', data.speciality);
            localStorage.setItem('InsCountry', data.insCountry);
            localStorage.setItem('Verified', data.verified);

            // If nonconventional, mark that mode in localStorage and redirect accordingly.
            if (data.nonConventional) {
                localStorage.setItem('NonConventionalMode', 'true');
                window.location.href = "/nonconventional.html";
            } else {
                window.location.href = '/SideBar/Instructions&Requirements/Instructions.html';
            }
        }
        catch (err) {
            console.error("Network or server error:", err);
            showError("Cannot connect to the server. Please try again.");
        }
    });

    function showError(msg) {
        const loginError = document.getElementById('loginError');
        if (loginError) {
            loginError.style.display = 'block';
            loginError.innerText = msg;
        } else {
            alert(msg);
        }
    }
});
