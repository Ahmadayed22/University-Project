document.addEventListener('DOMContentLoaded', () => {
    // Retrieve the Institution ID from localStorage
    const institutionId = localStorage.getItem('InsID');
    if (!institutionId) {
        console.warn("No Institution ID found. Redirecting to login...");
        window.location.href = "/login.html";
        return;
    }

    console.log("Page loaded. Fetching specializations...");
    fetchAllSpecializations(institutionId);

    attachFormSubmitListeners(institutionId);
});

// Function to fetch all specializations for the page
async function fetchAllSpecializations(institutionId) {
    console.log("Starting to fetch all specializations...");

    const formsToFetch = [
        { formId: "ScientificBachelorForm", insId: institutionId, type: "Scientific Bachelor" },
        { formId: "HumanitarianBachelorForm", insId: institutionId, type: "Humanitarian Bachelor" },
        { formId: "ScientificPracticalBachelorForm", insId: institutionId, type: "Scientific Practical Bachelor" },
        { formId: "HumanitarianPracticalBachelorForm", insId: institutionId, type: "Humanitarian Practical Bachelor" },
        { formId: "HighDiplomaForm", insId: institutionId, type: "High Diploma" },
        { formId: "ScientificMastersForm", insId: institutionId, type: "Scientific Masters" },
        { formId: "HumanitarianMastersForm", insId: institutionId, type: "Humanitarian Masters" },
        { formId: "ScientificPracticalMastersForm", insId: institutionId, type: "Scientific Practical Masters" },
        { formId: "MainMedicalForm", insId: institutionId, type: "Main Medical" },
        { formId: "ResidencyForm", insId: institutionId, type: "Residency" },
        { formId: "DoctorateForm", insId: institutionId, type: "Doctorate" }
    ];

    for (const { formId, insId, type } of formsToFetch) {
        console.log(`Fetching data for Form ID: ${formId}`);
        await fetchSpecialization(formId, insId, type);
    }

    console.log("Finished fetching all specializations.");
}

// Function to fetch a specific specialization and populate the form
async function fetchSpecialization(formId, insId, type) {
    console.log(`Fetching specialization for Form ID: ${formId}, Institution ID: ${insId}, Type: ${type}`);
    try {
        const response = await fetch(`/api/specialization/get-specialization/${insId}/${type}`);
        if (response.ok) {
            console.log(`Successfully fetched data for Form ID: ${formId}`);
            const data = await response.json();
            console.log(`Response data for Form ID: ${formId}:`, data);
            const form = document.getElementById(formId);
            if (!form) {
                console.error(`Form with ID: ${formId} not found.`);
                return;
            }

            // Populate form fields
            console.log(`Populating form fields for Form ID: ${formId}`);
            form.querySelector("#Type").value = data.specialization.type;
            form.querySelector("#NumStu").value = data.specialization.numStu;
            form.querySelector("#NumProf").value = data.specialization.numProf;
            form.querySelector("#NumAssociative").value = data.specialization.numAssociative;
            form.querySelector("#NumAssistant").value = data.specialization.numAssistant;
            form.querySelector("#NumPhdHolders").value = data.specialization.numPhdHolders;
            form.querySelector("#NumProfPractice").value = data.specialization.numProfPractice;
            form.querySelector("#NumberLecturers").value = data.specialization.numberLecturers;
            form.querySelector("#NumAssisLecturer").value = data.specialization.numAssisLecturer;
            form.querySelector("#NumOtherTeachers").value = data.specialization.numOtherTeachers;
            form.querySelector("#PracticalHours").value = data.specialization.practicalHours;
            form.querySelector("#TheoreticalHours").value = data.specialization.theoreticalHours;

            

            // Handle the Ratio and Color display
            console.log(`Updating Ratio display for Form ID: ${formId}`);
            const ratioDiv = form.querySelector("#Ratio");
            const ratioValue = data.specialization.ratio;
            const colorValue = data.specialization.color;

            // Set the text and color based on the ratio and color value
            ratioDiv.textContent = `Intake Capacity: ${ratioValue} : 1`;
            ratioDiv.style.textAlign = "center";
            ratioDiv.style.fontSize = "1.5rem";
            let colorClass = "";
            if (colorValue === 2) {
                colorClass = "green";
            } else if (colorValue === 1) {
                colorClass = "orange";
            } else if (colorValue === 0) {
                colorClass = "red";
            }
            ratioDiv.style.color = colorClass;

            // Display file links for all files in the response
            console.log(`Displaying file links for Form ID: ${formId}`);
            const fileKeys = Object.keys(data.files); // e.g., ['NumProfFile', 'NumAssociativeFile', ...]
            fileKeys.forEach((fileKey) => {
                const filePreview = form.querySelector(`#${fileKey}Preview`);
                if (filePreview) {
                    if (data.files[fileKey]) {
                        filePreview.innerHTML = `<a href="/${data.files[fileKey]}" target="_blank">View or Download File</a>`;
                        console.log(`File preview set for: ${fileKey}`);
                    } else {
                        filePreview.innerHTML = "No file available.";
                        console.log(`No file available for: ${fileKey}`);
                    }
                } else {
                    console.warn(`File preview container not found for key: ${fileKey}`);
                }
            });
        } else {
            const error = await response.json();
            console.error(`Error fetching data for Form ID: ${formId} - ${error.message}`);
           
        }
    } catch (err) {
        console.error(`Unexpected error fetching specialization for Form ID: ${formId}`, err);
    }
}




// Attach form submit listeners
function attachFormSubmitListeners(institutionId) {
    // List of form IDs and their corresponding endpoints
    const forms = [
        { id: 'ScientificBachelorForm', endpoint: '/api/specialization/save-scientific-bachelor' },
        /* Uncomment additional forms:
        { id: 'HumanitarianBachelorForm', endpoint: '/api/specialization/save-humanitarian-bachelor' },
        { id: 'ScientificPracticalBachelorForm', endpoint: '/api/specialization/save-scientific-practical-bachelor' },
        */
    ];

    forms.forEach(({ id, endpoint }) => {
        const form = document.getElementById(id);
        if (form) {
            form.addEventListener('submit', async function (e) {
                e.preventDefault();

                // Collect all form data (including files)
                const formData = new FormData(this);

                // Create the specialization object with numeric/text fields
                const specialization = {
                    Type: formData.get('Type'),
                    NumStu: formData.get('NumStu'),
                    NumProf: formData.get('NumProf'),
                    NumAssociative: formData.get('NumAssociative'),
                    NumAssistant: formData.get('NumAssistant'),
                    NumPhdHolders: formData.get('NumPhdHolders'),
                    NumProfPractice: formData.get('NumProfPractice'),
                    NumberLecturers: formData.get('NumberLecturers'),
                    NumAssisLecturer: formData.get('NumAssisLecturer'),
                    NumOtherTeachers: formData.get('NumOtherTeachers'),
                    InsID: institutionId,
                    PracticalHours: formData.get('PracticalHours'),
                    TheoreticalHours: formData.get('TheoreticalHours')
                };

                // Remove numeric/text fields from FormData to avoid duplication
                for (const key in specialization) {
                    formData.delete(key);
                }

                // Append JSON to FormData
                const jsonString = JSON.stringify(specialization);
                formData.append('specializationJson', jsonString);

                try {
                    // Make the POST request
                    const response = await fetch(endpoint, {
                        method: 'POST',
                        body: formData,
                        headers: {
                            'InstitutionID': institutionId
                        }
                    });

                    if (!response.ok) {
                        const error = await response.json();
                        alert(`Error: ${error.message || response.statusText}`);
                        return;
                    }

                    const data = await response.json();
                    console.log(`Response data for ${id}:`, data);

                    // Show color-coded result
                    let message;
                    switch (data.color) {
                        case 0:
                            message = "🔴 The ratio result is RED";
                            break;
                        case 1:
                            message = "🟠 The ratio result is ORANGE";
                            break;
                        case 2:
                            message = "🟢 The ratio result is GREEN";
                            break;
                        default:
                            message = "⚪ No color determined";
                            break;
                    }
                    alert(message);

                } catch (error) {
                    console.error(`Error submitting form ${id}:`, error);
                    alert('An error occurred while submitting the form. Please try again.');
                }
            });
        }
    });
}
