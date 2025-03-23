//document.getElementById('ScientificBachelorForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Gather the form data into an object
//    const formData = new FormData(this);
//    let object = {};
//    formData.forEach((value, key) => {
//        object[key] = value;
//    });

//    // Send as JSON since [FromBody] expects JSON
//    const response = await fetch('/api/specialization/save', {
//        method: 'POST',
//        headers: {
//            'Content-Type': 'application/json'
//        },
//        body: JSON.stringify(object)
//    });

//    if (response.ok) {
//        const jsonResponse = await response.json();
//        console.log(jsonResponse);

//        // Show a popup with an emoji indicating the color
//        let message;
//        switch (jsonResponse.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }

//        alert(message);
//    } else {
//        console.error('Error submitting form');
//        alert('Error submitting form because of' + response.statusText);
//    }
//});

document.addEventListener('DOMContentLoaded', () => {
    // Retrieve the Institution ID from localStorage
    const institutionId = localStorage.getItem('InsID');
    if (!institutionId) {
        console.warn("No Institution ID found. Redirecting to login...");
        window.location.href = "/login.html";
        return;
    }

    // List of form IDs and their corresponding endpoints
    const forms = [
        { id: 'ScientificBachelorForm', endpoint: '/api/specialization/save-scientific-bachelor' },
        { id: 'HumanitarianBachelorForm', endpoint: '/api/specialization/save-Humanitarian-Bachelor' },
        { id: 'ScientificPracticalBachelorForm', endpoint: '/api/specialization/save-Scientific-Practical-Bachelor' },
        { id: 'HumanitarianPracticalBachelorForm', endpoint: '/api/specialization/save-Humnamitarian-Practical-Bachelor' },
        { id: 'HighDiplomaForm', endpoint: '/api/specialization/save-High-Diploma' },
        { id: 'ScientificMastersForm', endpoint: '/api/specialization/save-Scientific-Masters' },
        { id: 'HumanitarianMastersForm', endpoint: '/api/specialization/save-Humanitarian-Masters' },
        { id: 'ScientificPracticalMastersForm', endpoint: '/api/specialization/save-Scientific-Practical-Masters' },
        { id: 'MainMedicalForm', endpoint: '/api/specialization/save-Main-Medical' },
        { id: 'ResidencyForm', endpoint: '/api/specialization/save-Residency' },
        { id: 'DoctorateForm', endpoint: '/api/specialization/save-Doctorate' }
    ];

    // Attach event listeners for each form
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
                        body: formData, // Send as multipart/form-data
                        headers: {
                            'InstitutionID': institutionId // Include the InstitutionID header
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
                    

                } catch (error) {
                    console.error(`Error submitting form ${id}:`, error);
                    alert('An error occurred while submitting the form. Please try again.');
                }
            });
        }
    });
});




//document.getElementById('ScientificPracticalBachelorForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Scientific-Practical-Bachelor', {
//            method: 'POST',
//            body: formData,
//            headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});
//document.getElementById('HumnamitarianPracticalBachelorForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission
//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Humnamitarian-Practical-Bachelor', {
//            method: 'POST',
//            body: formData, headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});





//document.getElementById('HighDiplomaForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-High-Diploma', {
//            method: 'POST',
//            body: formData, headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});
//document.getElementById('ScientificMastersForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Scientific-Masters', {
//            method: 'POST',
//            body: formData,
//            headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});
//document.getElementById('HumanitarianMastersForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Humanitarian-Masters', {
//            method: 'POST',
//            body: formData,
//            headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});
//document.getElementById('ScientificPracticalMastersForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Scientific-Practical-Masters', {
//            method: 'POST',
//            body: formData,
//            headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});
//document.getElementById('MainMedicalForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission
//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Main-Medical', {
//            method: 'POST',
//            body: formData,
//            headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});
//document.getElementById('ResidencyForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Residency', {
//            method: 'POST',
//            body: formData,
//            headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});

//document.getElementById('DoctorateForm').addEventListener('submit', async function (e) {
//    e.preventDefault(); // Prevent the default form submission

//    // Collect all form data (including files)
//    const formData = new FormData(this);

//    // We'll keep the numeric fields and "Type" in a JSON object
//    const specialization = {
//        Type: formData.get('Type'),          // "Scientific Bachelor"
//        NumStu: formData.get('NumStu'),
//        NumProf: formData.get('NumProf'),
//        NumAssociative: formData.get('NumAssociative'),
//        NumAssistant: formData.get('NumAssistant'),
//        NumPhdHolders: formData.get('NumPhdHolders'),
//        NumProfPractice: formData.get('NumProfPractice'),
//        NumberLecturers: formData.get('NumberLecturers'),
//        NumAssisLecturer: formData.get('NumAssisLecturer'),
//        NumOtherTeachers: formData.get('NumOtherTeachers'),
//        InsID: formData.get('InsID'),
//        PracticalHours: formData.get('PracticalHours'),
//        TheoreticalHours: formData.get('TheoreticalHours')
//    };

//    // Remove those JSON fields from FormData, because we'll send them as a single JSON string
//    // so we don't double-send them. (Optional, but keeps your request clean.)
//    formData.delete('Type');
//    formData.delete('NumStu');
//    formData.delete('NumProf');
//    formData.delete('NumAssociative');
//    formData.delete('NumAssistant');
//    formData.delete('NumPhdHolders');
//    formData.delete('NumProfPractice');
//    formData.delete('NumberLecturers');
//    formData.delete('NumAssisLecturer');
//    formData.delete('NumOtherTeachers');
//    formData.delete('InsID');
//    formData.delete('PracticalHours');
//    formData.delete('TheoreticalHours');

//    // Convert the specialization object to JSON
//    const jsonString = JSON.stringify(specialization);

//    // Append that JSON to FormData under the key "specializationJson"
//    formData.append('specializationJson', jsonString);

//    // Now FormData still has the file fields, e.g. "NumProfFile", "NumAssociativeFile", etc.
//    // plus our "specializationJson" field

//    // Make the POST request to the new endpoint
//    try {
//        const response = await fetch('/api/specialization/save-Doctorate', {
//            method: 'POST',
//            body: formData,
//            headers: {
//                'InstitutionID': institutionId
//        });

//        if (!response.ok) {
//            const error = await response.json();
//            alert(`Error: ${error.message || response.statusText}`);
//            return;
//        }

//        const data = await response.json();
//        console.log('Response data:', data);

//        // Show color-coded result if needed
//        let message;
//        switch (data.color) {
//            case 0:
//                message = "🔴 The ratio result is RED";
//                break;
//            case 1:
//                message = "🟠 The ratio result is ORANGE";
//                break;
//            case 2:
//                message = "🟢 The ratio result is GREEN";
//                break;
//            default:
//                message = "⚪ No color determined";
//                break;
//        }
//        alert(message);

//    } catch (error) {
//        console.error('Error submitting form:', error);
//        alert('An error occurred while submitting the form. Please try again.');
//    }
//});

