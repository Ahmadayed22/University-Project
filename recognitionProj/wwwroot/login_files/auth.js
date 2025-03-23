//IMPORTANT!!! THIS IS NOT SECURE, IT IS JUST A SIMPLE CHECK TO SEE IF THE USER IS LOGGED IN



document.addEventListener("DOMContentLoaded", () => {
    const lastLogin = localStorage.getItem("lastLoginTime");
    const oneDayMillis = 24 * 60 * 60 * 1000; // 86400000
    const now = Date.now();
    const isAdmin = localStorage.getItem("isAdmin") === "true"; // Ensure it is checked as a boolean

    if (lastLogin) {
        const diff = now - parseInt(lastLogin, 10);
        if (diff > oneDayMillis && !isAdmin) {
            console.warn("Stored data is older than 1 day. Clearing localStorage now.");
            localStorage.clear();
            window.location.href = "/login.html"; // Redirect to login
            return;
        }
    } else {
        console.log("No lastLoginTime found, setting it now.");
        localStorage.setItem("lastLoginTime", now.toString());
    }

    const insID = localStorage.getItem("InsID"); // normal institution
    const supervisorID = localStorage.getItem("SupervisorID"); // supervisor

    if (!insID && !supervisorID && !isAdmin) {
        console.warn("No InsID or SupervisorID found and user is not admin. Redirecting...");
        window.location.href = "/login.html"; // Redirect to login page
        return;
    }

    console.log("Auth check passed: user is either InsID, SupervisorID, or Admin; not kicking out.");
});
