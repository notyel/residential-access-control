from playwright.sync_api import Page, expect

def test_redesigned_dashboard(page: Page):
    """
    This test verifies that the redesigned dashboard is displayed correctly
    and that the theme can be toggled.
    """
    # 1. Arrange: Go to the dashboard page.
    page.goto("http://localhost:4200")

    # 2. Assert: Check for the new components.
    expect(page.locator('app-header')).to_be_visible()
    expect(page.locator('app-sidebar')).to_be_visible()
    expect(page.locator('app-stats-card')).to_have_count(4)
    expect(page.locator('app-user-list')).to_be_visible()

    # 3. Screenshot: Capture the light mode view.
    page.screenshot(path="jules-scratch-2/verification/dashboard-redesigned-light.png")

    # 4. Act: Find the theme toggle button and click it.
    theme_toggle_button = page.locator('.theme-toggle-btn')
    theme_toggle_button.click()

    # 5. Assert: Check that the theme has changed to dark.
    expect(page.locator('body')).to_have_attribute('data-theme', 'dark')

    # 6. Screenshot: Capture the dark mode view.
    page.screenshot(path="jules-scratch-2/verification/dashboard-redesigned-dark.png")
