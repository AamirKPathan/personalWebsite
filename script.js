// ============================================
// PERSONAL PORTFOLIO - JAVASCRIPT
// ============================================

// Make scrollToSection globally available immediately
window.scrollToSection = function(sectionId) {
    const section = document.getElementById(sectionId);
    if (section) {
        section.scrollIntoView({ behavior: 'smooth' });
        updateActiveLink(sectionId);
    }
};

// Update active navigation link
function updateActiveLink(sectionId) {
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.classList.remove('active');
        if (link.getAttribute('data-section') === sectionId) {
            link.classList.add('active');
        }
    });
}

// Toggle project details modal - GLOBAL FUNCTION
window.toggleProjectDetails = function(element) {
    console.log('toggleProjectDetails called', element);
    
    // Find the project card (could be button or card itself)
    let card = element;
    if (element.classList && !element.classList.contains('project-card')) {
        card = element.closest('.project-card');
    }
    
    if (!card) {
        console.error('Could not find project card');
        return;
    }
    
    console.log('Card found:', card);
    
    // Close any other expanded cards
    const allCards = document.querySelectorAll('.project-card.expanded');
    allCards.forEach(c => {
        if (c !== card) {
            c.classList.remove('expanded');
        }
    });

    // Toggle current card
    const isExpanded = card.classList.contains('expanded');
    console.log('Is expanded before toggle:', isExpanded);
    
    if (isExpanded) {
        card.classList.remove('expanded');
        document.body.classList.remove('modal-open');
    } else {
        card.classList.add('expanded');
        document.body.classList.add('modal-open');
    }
    
    console.log('Is expanded after toggle:', card.classList.contains('expanded'));
};

// Close modal when clicking outside
document.addEventListener('click', function(e) {
    const expandedCard = document.querySelector('.project-card.expanded');
    if (expandedCard && !expandedCard.contains(e.target)) {
        expandedCard.classList.remove('expanded');
        document.body.classList.remove('modal-open');
    }
});

// Close modal on Escape key
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        const expandedCard = document.querySelector('.project-card.expanded');
        if (expandedCard) {
            expandedCard.classList.remove('expanded');
            document.body.classList.remove('modal-open');
        }
    }
});

// Initialize navigation when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM Content Loaded');
    
    const navLinks = document.querySelectorAll('.nav-link');
    const sections = document.querySelectorAll('.section');

    // Handle navigation link clicks
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const sectionId = this.getAttribute('data-section');
            window.scrollToSection(sectionId);
        });
    });

    // Update active nav link on scroll
    window.addEventListener('scroll', function() {
        let currentSection = '';

        sections.forEach(section => {
            const sectionTop = section.offsetTop;
            if (window.pageYOffset >= sectionTop - 200) {
                currentSection = section.getAttribute('id');
            }
        });

        navLinks.forEach(link => {
            link.classList.remove('active');
            if (link.getAttribute('data-section') === currentSection) {
                link.classList.add('active');
            }
        });
    });
});

// Handle contact form submission
window.handleContactForm = function(event) {
    event.preventDefault();
    
    const form = event.target;
    const name = document.getElementById('name').value;
    const email = document.getElementById('email').value;
    const message = document.getElementById('message').value;
    
    // Show success message
    const submitBtn = form.querySelector('button[type="submit"]');
    const originalText = submitBtn.textContent;
    
    submitBtn.textContent = 'Message Sent! ✓';
    submitBtn.style.backgroundColor = 'var(--accent-secondary)';
    
    // Reset form
    form.reset();
    
    // Restore button after 3 seconds
    setTimeout(() => {
        submitBtn.textContent = originalText;
        submitBtn.style.backgroundColor = '';
    }, 3000);
    
    // In a real application, you would send this data to a server
    console.log('Contact form submitted:', { name, email, message });
};
