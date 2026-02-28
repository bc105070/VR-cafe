# TUM-MGTHN0183-Cafe: Virtual Reality Experimental Environment

## 1. Overview
This repository contains the Unity-based Virtual Reality (VR) application developed for an experimental study on consumer behavior and human-computer interaction in a simulated service environment. The application serves as an experimental apparatus designed to investigate the effects of environmental variables—specifically visual tone and auditory stimuli—on participant decision-making and interaction patterns with a virtual embodied agent.

## 2. Experimental Design
The study employs a 2x2 between-subjects factorial design. The controlled independent variables (IVs) are:
*   **Visual Environment (Color Temperature):** Manipulated via Universal Render Pipeline (URP) Post-Processing volumes, yielding two levels: 'Warm' (+20f color temperature correction) and 'Cold' (-20f color temperature correction).
*   **Auditory Stimuli:** Operationalized through two distinct voice audio profiles utilized by the virtual agent (Audio Set 0 vs. Audio Set 1).

Participants ($N=24$) are pre-assigned to one of the four experimental conditions. The operationalization of these conditions is automated; the application dynamically configures the corresponding parameters upon initialization based on the input Participant ID.

## 3. Software Architecture
The application is structured to ensure systematic phase transitions, accurate stimuli presentation, and reliable data acquisition. Key system modules include:

*   **`StateManagement.cs`**: The central controller governing the trial's sequential phase progression (Greeting, Ordering, Confirmation, Conclusion). It executes the dynamic assignment of condition-specific visual and auditory parameters.
*   **`ExperimentSession.cs`**: A persistent state manager (implementing the Singleton pattern) that retains participant metadata and observational data across scene transitions without data loss.
*   **`AgentDestinationSetter.cs`**: Governs the spatial navigation (via Unity NavMesh) and behavior of the virtual agent (waiter). This module synchronizes the agent's locomotion, kinematic orientation towards the user, animation states, and audio playback to ensure high experimental realism.
*   **`CSVWriter.cs`**: A dedicated I/O module handling the real-time, non-blocking transcription of dependent variables into structured local data files.

## 4. Experimental Procedure
The simulated user interaction is systematically structured into four distinct phases to standardize the participant experience:
1.  **Approach and Greeting:** The virtual agent navigates to the participant's spatial coordinates, establishes visual alignment, and delivers the condition-specific greeting.
2.  **Menu Presentation and Ordering:** A spatial user interface (UI) materializes, presenting the choice set. Participants indicate their selection via VR input semantics.
3.  **Order Confirmation:** The system registers the input, and the virtual agent aurally confirms the received order.
4.  **Conclusion and Data Logging:** The trial terminates, and behavioral data is instantaneously committed to local storage to prevent data attrition.

## 5. Data Acquisition
Following the conclusion of each experimental session, the system automatically appends a discrete data record to `Participants.csv`. The exported dataset comprises the following schema:
*   `ParticipantID`: The numerical identifier assigned to the subject.
*   `Condition`: The assigned experimental cohort (integer values 1–4).
*   `OrderChoice`: The categorical dependent variable representing the participant's specific menu selection (e.g., "Set1").
*   `Timestamp`: Temporal marker of the recorded trial completion (`yyyy-MM-dd HH:mm:ss`).

## 6. Technical Requirements & Deployment
*   **Development Environment:** Unity version 6000.2.8f1 (or compatible LTS).
*   **Rendering Pipeline:** Universal Render Pipeline (URP) is mandatory for volumetric and color grading manipulations.
*   **Hardware Compatibility:** Engineered for OpenXR-compatible Head-Mounted Displays (HMDs), such as the Meta Quest series.
*   **Initialization:** Deployment requires the instantiation of the `PlayerPrefs` variable `ParticipantID` prior to loading the main experimental scene to ensure correct cohort allocation.
