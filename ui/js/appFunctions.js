function generateTimeline(inputString) {
    const timelineVisualization = document.getElementById('timelineVisualization');

    // Loop through each character in the input string
    for (let i = 0; i < inputString.length; i++) {
        const char = inputString[i];
        let timelineItem = ''; // Initialize to an empty string

        // Determine the appropriate class based on the character
        switch (char) {
            case 'N':
                timelineItem = '<div class="blank-keyframe"></div>';
                break;
            case 'E':
                timelineItem = '<div class="empty-frame"></div>';
                break;
            case 'K':
                timelineItem = '<div class="keyframe"></div>';
                break;
            case 'F':
                timelineItem = '<div class="frame"></div>';
                break;
            default:
                // Handle other characters if needed
                break;
        }

        // Append the timeline item to the timeline visualization
        timelineVisualization.innerHTML += timelineItem;
    }
}