document.addEventListener('DOMContentLoaded', async function () {
    const libraryForm = document.getElementById('libraryForm');

    if (!libraryForm) return;

    const InsID = localStorage.getItem("InsID");

    if (InsID) {
        try {
            const getResponse = await fetch(`/api/library/getbyid/${InsID}`);
            if (getResponse.ok) {
                const resultJson = await getResponse.json();
                console.log("Library info received:", resultJson);

                if (resultJson.success) {
                    const data = resultJson.data;
                    document.getElementById('InsID').value = data.insID || '';
                    document.getElementById('Area').value = data.area || '';
                    document.getElementById('Capacity').value = data.capacity || '';
                    document.getElementById('ArBooks').value = data.arBooks || '';
                    document.getElementById('EnBooks').value = data.enBooks || '';
                    document.getElementById('Papers').value = data.papers || '';
                    document.getElementById('EBooks').value = data.eBooks || '';
                    document.getElementById('ESubscription').value = data.eSubscription || '';
                }
            }
        } catch (error) {
            console.error("Error fetching Library Info:", error);
        }
    }

    libraryForm.addEventListener('submit', async function (event) {
        event.preventDefault();

        const libraryInfo = {
            insID: parseInt(document.getElementById('InsID').value) || 0,
            area: parseInt(document.getElementById('Area').value) || null,
            capacity: parseInt(document.getElementById('Capacity').value) || null,
            arBooks: parseInt(document.getElementById('ArBooks').value) || null,
            enBooks: parseInt(document.getElementById('EnBooks').value) || null,
            papers: parseInt(document.getElementById('Papers').value) || null,
            eBooks: parseInt(document.getElementById('EBooks').value) || null,
            eSubscription: parseInt(document.getElementById('ESubscription').value) || null
        };

        try {
            const response = await fetch('/api/library/save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(libraryInfo)
            });

            if (response.ok) {
                alert("Library information saved successfully!");
                window.location.href = "/attachments.html";
            }
        } catch (error) {
            console.error("Error saving Library Info:", error);
        }
    });
});
