import sys
from playwright.sync_api import Page, expect, Error

def test_dashboard_theme_toggle(page: Page):
    """
    This test verifies that the dashboard theme can be toggled
    between light and dark mode.
    """
    with open("../jules-scratch/verification/playwright_output.log", "w") as f:
        try:
            # 1. Arrange: Go to the dashboard page.
            f.write("Navigating to http://localhost:4200...\n")
            page.goto("http://localhost:4200", timeout=30000)
            f.write(f"Page title: {page.title()}\n")

            # 2. Assert: Check that the initial theme is light.
            f.write("Checking for light theme...\n")
            expect(page.locator('body')).to_have_attribute('data-theme', 'light', timeout=10000)
            f.write("Light theme found.\n")

            # 3. Screenshot: Capture the light mode view.
            f.write("Taking light mode screenshot...\n")
            page.screenshot(path="../jules-scratch/verification/dashboard-light-mode.png")
            f.write("Light mode screenshot taken.\n")

            # 4. Act: Find the theme toggle button and click it.
            f.write("Clicking theme toggle button...\n")
            theme_toggle_button = page.get_by_text("🌜")
            theme_toggle_button.click()
            f.write("Theme toggle button clicked.\n")

            # 5. Assert: Check that the theme has changed to dark.
            f.write("Checking for dark theme...\n")
            expect(page.locator('body')).to_have_attribute('data-theme', 'dark', timeout=10000)
            f.write("Dark theme found.\n")

            # 6. Screenshot: Capture the dark mode view.
            f.write("Taking dark mode screenshot...\n")
            page.screenshot(path="../jules-scratch/verification/dashboard-dark-mode.png")
            f.write("Dark mode screenshot taken.\n")

        except Error as e:
            f.write(f"An error occurred: {e}\n")
            sys.exit(1)
