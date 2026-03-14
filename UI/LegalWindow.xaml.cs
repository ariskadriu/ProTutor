using System.Windows;

namespace ProgrammingTutor.UI
{
    public partial class LegalWindow : Window
    {
        public LegalWindow(string type)
        {
            InitializeComponent();
            LoadContent(type);
        }

        private void LoadContent(string type)
        {
            if (type == "privacy")
            {
                LegalHeader.Text = "Privacy Policy";
                LegalText.Text = @"PROTECTION OF PERSONAL DATA

1. Introduction
I, Aris Kadriu ('I', 'me', or 'my'), as an individual developer, am committed to protecting your personal data in the Pro-Tutor Programming Learning Platform.

2. The Data I Collect
I collect the following personal data:
- Name and Surname: For personalized certificate generation.
- Username: For account identification.
- Training Progress: To save your journey through lessons.
- Performance Points: For the global leaderboard.

3. Online Features & Transmission
While most data is stored locally, your Username, Points, and Rank are transmitted to a public Firebase database to power the Online Leaderboard. Your Name and Surname are NEVER transmitted online and remain strictly on your local device.

4. Data Security
I use industry-standard hashing (SHA256 with random salts) to protect your passwords locally. The Online Leaderboard uses validation handshakes to ensure data integrity.

© 2026 Aris Kadriu";
            }
            else if (type == "terms")
            {
                LegalHeader.Text = "Terms of Service";
                LegalText.Text = @"TERMS AND CONDITIONS

1. Acceptance of Terms
By using Pro-Tutor, you agree to these terms.

2. Educational Use
Pro-Tutor and the 'Aris AI' hint system are provided for educational purposes only. AI hints are generated based on local heuristics and are not guaranteed to be 100% accurate.

3. Online Leaderboard
By participating in challenges, you consent to your Username and Scores being visible to other users globally via the leaderboard.

4. Intellectual Property
All content, including the custom W3Schools-inspired curriculum, is the intellectual property of Aris Kadriu. 

5. Limitation of Liability
I, Aris Kadriu, am not liable for any system issues arising from code execution within the app's sandbox environment.

© 2026 Aris Kadriu";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
