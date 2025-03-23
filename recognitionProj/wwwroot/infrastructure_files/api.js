document.addEventListener('DOMContentLoaded', function () {
    const infrastructureForm = document.getElementById('infrastructureForm');
    if (!infrastructureForm) return; // Safety check if form is missing

    infrastructureForm.addEventListener('submit', async function (event) {
        event.preventDefault(); // Prevent page reload

        const formData = new FormData();

        // Get Institution ID (you need to set this dynamically, e.g., from user session or input field)
        const institutionId = document.getElementById('InsID').value; // Assume an input field exists

        if (!institutionId) {
            alert("Institution ID is required!");
            return;
        }

        // Prepare JSON Data (convert to string)
        const infrastructureData = {
            InsID: document.getElementById('InsID').value || 0,
            Area: document.getElementById('Area').value || null,
            Sites: document.getElementById('Sites').value || null,
            Terr: document.getElementById('Terr').value || null,
            Halls: document.getElementById('Halls').value || null,
            Library: document.getElementById('Library').value || null
        };
        formData.append("infrastructureJson", JSON.stringify(infrastructureData)); // Convert to string

        // Validate and add files
        function validateFile(file, allowedExtensions) {
            if (!file) return false;
            const fileExt = file.name.split('.').pop().toLowerCase();
            return allowedExtensions.includes(fileExt);
        }

        const allowedExtensions = ["pdf", "jpg", "png"];
        const labsAllowedExtensions = ["xls", "xlsx","csv"]; // ✅ Only allow Excel files for Labs
        const areaFile = document.getElementById('AreaFile').files[0];
        if (validateFile(areaFile, allowedExtensions)) {
            formData.append("AreaFile", areaFile);
        } else if (areaFile) {
            alert("Invalid AreaFile type! Allowed: .pdf, .jpg, .png");
            return;
        }

        const labsFile = document.getElementById('LabsFile').files[0];
        if (validateFile(labsFile, labsAllowedExtensions)) {
            formData.append("LabsFile", labsFile);
        } else if (labsFile) {
            alert("Invalid LabsFile type! Allowed: .xlsx .xls .csv");
            return;
        }

        try {
            const response = await fetch('/api/infrastructure/save', {
                method: 'POST',
                body: formData, // Automatically sets the correct Content-Type
                headers: {
                    'InstitutionID': institutionId // ✅ Send Institution ID in the headers
                }
            });

            if (!response.ok) {
                const errorData = await response.json();
                console.error('Error:', errorData);
                alert(`Error: ${errorData.message || 'Unable to save infrastructure information.'}`);
                return;
            }

            const result = await response.json();
            console.log("Success:", result);
            alert(`Success: ${result.message}`);
            window.location.href = "/library.html";
            // Optionally reset the form
            // infrastructureForm.reset();
        } catch (error) {
            console.error('Network error:', error);
            alert(`Network error: ${error.message}`);
        }
    });
});
