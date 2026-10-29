#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Libs.KeyboardAvoidance;
using UnityEngine;
using UnityEngine.UIElements;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="KeyboardDisplacement"/>.
    /// The shift is the smallest upward move that clears the focused field, measured from the unshifted layout.
    /// </summary>
    internal sealed class KeyboardDisplacementTests
    {
        [Test]
        public void When_field_is_above_keyboard_expect_no_shift()
        {
            var result = Calculate(Field(0f, 200f, 100f, 48f), Occlusion(0f, 620f, 400f, 400f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(0f),
                "A field that already clears the keyboard, including the margin, should not move.");
            Assert.That(result.Status, Is.EqualTo(KeyboardAvoidanceStatus.VisibleWithoutAdjustment));
        }

        [Test]
        public void When_field_touches_keyboard_with_no_margin_expect_no_shift()
        {
            var result = Calculate(Field(0f, 572f, 100f, 48f), Occlusion(0f, 620f, 400f, 400f), margin: 0f);

            Assert.That(result.UpwardShift, Is.EqualTo(0f),
                "A field whose bottom touches the keyboard top needs no extra movement when the margin is zero.");
        }

        [Test]
        public void When_field_touches_keyboard_expect_margin_shift()
        {
            var result = Calculate(Field(0f, 572f, 100f, 48f), Occlusion(0f, 620f, 400f, 400f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(12f).Within(0.01f),
                "Touching the keyboard should move the field by the requested margin.");
        }

        [Test]
        public void When_field_overlaps_keyboard_expect_spec_example_shift()
        {
            // Field bottom 760, keyboard top 620, margin 12. S = 760 - 620 + 12 = 152.
            var result = Calculate(Field(0f, 712f, 200f, 48f), Occlusion(0f, 620f, 400f, 500f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(152f).Within(0.01f),
                "The field should move by exactly the overlap plus the margin.");
            Assert.That(result.Status, Is.EqualTo(KeyboardAvoidanceStatus.Avoiding));
        }

        [Test]
        public void When_field_is_fully_inside_keyboard_expect_shift_that_clears_its_bottom()
        {
            var result = Calculate(Field(0f, 700f, 100f, 60f), Occlusion(0f, 400f, 400f, 800f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(372f).Within(0.01f),
                "A covered field should move until its bottom sits one margin above the keyboard.");
            Assert.That(result.Status, Is.EqualTo(KeyboardAvoidanceStatus.Avoiding));
        }

        [Test]
        public void When_field_is_taller_than_the_open_area_expect_constrained_shift()
        {
            var result = Calculate(Field(0f, 0f, 100f, 700f), Occlusion(0f, 620f, 400f, 400f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(92f).Within(0.01f),
                "The shift should still clear the field bottom and must not grow without limit.");
            Assert.That(result.Status, Is.EqualTo(KeyboardAvoidanceStatus.Constrained));
            Assert.That(result.Message, Is.EqualTo(KeyboardDisplacement.FieldTooTallMessage));
        }

        [Test]
        public void When_keyboard_does_not_overlap_horizontally_expect_no_shift()
        {
            var result = Calculate(Field(0f, 700f, 80f, 60f), Occlusion(200f, 400f, 100f, 400f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(0f),
                "A keyboard beside the field should not move the field.");
        }

        [Test]
        public void When_keyboard_overlaps_part_of_the_field_width_expect_shift()
        {
            var result = Calculate(Field(0f, 712f, 100f, 48f), Occlusion(80f, 620f, 120f, 400f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(152f).Within(0.01f),
                "A partial horizontal overlap should still clear the field.");
        }

        [Test]
        public void When_field_is_below_a_floating_keyboard_expect_no_shift()
        {
            var result = Calculate(Field(0f, 800f, 100f, 60f), Occlusion(40f, 400f, 120f, 100f), margin: 12f);

            Assert.That(result.UpwardShift, Is.EqualTo(0f),
                "A floating keyboard that does not cover the field should not pull the field upward.");
        }

        [Test]
        public void When_margin_changes_expect_shift_to_change_by_the_same_amount()
        {
            var field = Field(0f, 712f, 200f, 48f);
            var keyboard = Occlusion(0f, 620f, 400f, 500f);

            var noMargin = Calculate(field, keyboard, margin: 0f);
            var largerMargin = Calculate(field, keyboard, margin: 20f);

            Assert.That(noMargin.UpwardShift, Is.EqualTo(140f).Within(0.01f));
            Assert.That(largerMargin.UpwardShift, Is.EqualTo(160f).Within(0.01f));
        }

        [Test]
        public void When_translation_limit_is_reached_expect_capped_shift()
        {
            var result = Calculate(Field(0f, 712f, 200f, 48f), Occlusion(0f, 620f, 400f, 500f), margin: 12f, 50f);

            Assert.That(result.UpwardShift, Is.EqualTo(50f).Within(0.01f),
                "The configured maximum translation should cap the movement.");
            Assert.That(result.Status, Is.EqualTo(KeyboardAvoidanceStatus.Constrained));
            Assert.That(result.Message, Is.EqualTo(KeyboardDisplacement.MaximumTranslationMessage));
        }

        [Test]
        public void When_recalculated_from_the_shifted_rect_expect_the_same_absolute_shift()
        {
            var baseline = Field(0f, 712f, 200f, 48f);
            var keyboard = Occlusion(0f, 620f, 400f, 500f);
            var first = Calculate(baseline, keyboard, margin: 12f);
            var measured = new PanelRect(
                baseline.XMin,
                baseline.YMin - first.UpwardShift,
                baseline.XMax,
                baseline.YMax - first.UpwardShift);

            var second = Calculate(measured.UnshiftedUp(first.UpwardShift), keyboard, margin: 12f);

            Assert.That(second.UpwardShift, Is.EqualTo(first.UpwardShift).Within(0.01f),
                "Recomputing from a translated field must not add the shift a second time.");
        }

        [Test]
        public void When_the_same_rect_is_calculated_twice_expect_the_same_shift()
        {
            var field = Field(0f, 712f, 200f, 48f);
            var keyboard = Occlusion(0f, 620f, 400f, 500f);

            var first = Calculate(field, keyboard, margin: 12f);
            var second = Calculate(field, keyboard, margin: 12f);

            Assert.That(second.UpwardShift, Is.EqualTo(first.UpwardShift).Within(0.01f),
                "Repeating an identical measurement should return the same absolute shift.");
        }

        private static KeyboardDisplacementResult Calculate(PanelRect field, PanelRect occlusion, float margin,
            float? maximumTranslation = null)
            => KeyboardDisplacement.Calculate(field, occlusion, margin, maximumTranslation, viewportTop: 0f);

        private static PanelRect Field(float x, float y, float width, float height)
            => new PanelRect(x, y, x + width, y + height);

        private static PanelRect Occlusion(float x, float y, float width, float height)
            => new PanelRect(x, y, x + width, y + height);
    }

    /// <summary>
    /// Tests for <see cref="KeyboardOcclusion"/> and <see cref="PanelCoordinateConversion"/>.
    /// </summary>
    internal sealed class KeyboardOcclusionTests
    {
        [Test]
        public void When_keyboard_is_outside_the_resized_view_expect_empty_overlap()
        {
            // Window is 2400px tall and the keyboard covers the bottom 800. The Unity view was resized to 1600.
            var valid = KeyboardOcclusion.TryNormalizeIntersection(
                0f, 1600f, 1080f, 2400f,
                0f, 0f, 1080f, 1600f,
                out var normalized);

            Assert.That(valid, Is.True, "A resized view is a valid measurement.");
            Assert.That(normalized.IsEmpty, Is.True,
                "A keyboard that sits below the resized Unity view should not count as overlap.");
        }

        [Test]
        public void When_panel_stays_full_height_expect_the_keyboard_to_cover_that_panel()
        {
            // The native view shrunk to 1600, but UI Toolkit is still laid out on a 2400px screen.
            var covered = KeyboardOcclusion.BottomCoveredPixels(0, 2400, 1600);

            Assert.That(covered, Is.EqualTo(800),
                "The keyboard still covers the part of the full-height panel that extends below the visible frame.");
        }

        [Test]
        public void When_panel_shrinks_with_the_visible_frame_expect_no_extra_cover()
        {
            var covered = KeyboardOcclusion.BottomCoveredPixels(0, 1600, 1600);

            Assert.That(covered, Is.EqualTo(0),
                "A panel that already ends at the visible frame does not need another keyboard shift.");
        }

        [Test]
        public void When_keyboard_covers_the_bottom_of_the_view_expect_normalized_fraction()
        {
            var valid = KeyboardOcclusion.TryNormalizeIntersection(
                0f, 1600f, 1080f, 2400f,
                0f, 0f, 1080f, 2400f,
                out var normalized);

            Assert.That(valid, Is.True);
            Assert.That(normalized.Y, Is.EqualTo(1600f / 2400f).Within(0.0001f));
            Assert.That(normalized.Height, Is.EqualTo(800f / 2400f).Within(0.0001f));
            Assert.That(normalized.Width, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void When_bottom_left_keyboard_rect_is_converted_expect_top_left_fraction()
        {
            var valid = KeyboardOcclusion.TryFromBottomLeftScreenRect(new Rect(0f, 0f, 1000f, 800f), 1000f, 2000f,
                out var normalized);

            Assert.That(valid, Is.True);
            Assert.That(normalized.Y, Is.EqualTo(0.6f).Within(0.0001f),
                "An 800px keyboard on a 2000px screen starts at 60% of the height from the top.");
            Assert.That(normalized.Height, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void When_view_size_is_invalid_expect_measurement_rejected()
        {
            var valid = KeyboardOcclusion.TryNormalizeIntersection(0f, 0f, 10f, 10f, 0f, 0f, 0f, 100f, out _);

            Assert.That(valid, Is.False, "A zero-sized view cannot be turned into a keyboard fraction.");
        }

        [Test]
        public void When_normalized_rect_is_scaled_into_panel_space_expect_both_corners_scaled()
        {
            var panel = PanelCoordinateConversion.ToPanelRect(
                new NormalizedRect(0f, 0.7f, 1f, 0.3f),
                200f,
                1000f,
                point => point / 2f);

            Assert.That(panel.YMin, Is.EqualTo(350f).Within(0.01f));
            Assert.That(panel.YMax, Is.EqualTo(500f).Within(0.01f));
            Assert.That(panel.XMax, Is.EqualTo(100f).Within(0.01f));
        }
    }

    /// <summary>
    /// Tests for <see cref="KeyboardGeometryService"/> revision tracking.
    /// </summary>
    internal sealed class KeyboardGeometryServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            _provider = new SimulatedKeyboardGeometryProvider();
            _service = KeyboardGeometryService.CreateStandalone(_provider);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        [Test]
        public void When_the_same_snapshot_is_published_twice_expect_one_revision()
        {
            var hidden = KeyboardGeometrySnapshot.Hidden(KeyboardProviderId.Simulated);
            _provider.Set(hidden);
            _service.Poll();
            _provider.Set(hidden);
            _service.Poll();

            Assert.That(_service.Revision, Is.EqualTo(1),
                "An identical measurement should not look like a new keyboard change.");
            Assert.That(_service.Current.State, Is.EqualTo(KeyboardVisibility.Hidden));
            Assert.That(_service.Current.IsValid, Is.True);
        }

        [Test]
        public void When_geometry_changes_expect_revision_to_increase()
        {
            _provider.Set(KeyboardGeometrySnapshot.Hidden(KeyboardProviderId.Simulated));
            _service.Poll();
            var occlusions = new KeyboardOcclusionList();
            occlusions.Add(new NormalizedRect(0f, 0.6f, 1f, 0.4f));
            _provider.Set(KeyboardGeometrySnapshot.Create(KeyboardVisibility.Visible, true, KeyboardProviderId.Simulated,
                occlusions));
            _service.Poll();

            Assert.That(_service.Revision, Is.EqualTo(2));
            Assert.That(_service.Current.OcclusionCount, Is.EqualTo(1));
            Assert.That(_service.Current.Occlusion(0).Y, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void When_reacquire_repeats_the_same_geometry_expect_a_new_revision()
        {
            _provider.Set(KeyboardGeometrySnapshot.Hidden(KeyboardProviderId.Simulated));
            _service.Poll();

            _provider.Reacquire();
            _service.Poll();

            Assert.That(_service.Revision, Is.EqualTo(2),
                "Resuming should refresh the measurement even when the keyboard did not move.");
        }

        private SimulatedKeyboardGeometryProvider _provider;
        private KeyboardGeometryService _service;
    }

    /// <summary>
    /// Tests for <see cref="TextFieldFocus"/>.
    /// </summary>
    internal sealed class TextFieldFocusTests
    {
        [Test]
        public void When_focus_is_an_internal_child_expect_owning_text_field()
        {
            var field = new TextField();
            var child = new VisualElement();
            field.Add(child);

            Assert.That(TextFieldFocus.FindTextField(child), Is.SameAs(field),
                "Focus inside a text field should resolve to that field.");
        }

        [Test]
        public void When_element_is_not_a_text_field_expect_no_match()
        {
            var element = new VisualElement();

            Assert.That(TextFieldFocus.FindTextField(element), Is.Null);
        }

        [Test]
        public void When_field_is_removed_expect_it_outside_the_root()
        {
            var root = new VisualElement();
            var field = new TextField();
            root.Add(field);
            field.RemoveFromHierarchy();

            Assert.That(TextFieldFocus.IsEligible(field, root, root, null), Is.False,
                "A detached field should stop affecting avoidance.");
        }

        [Test]
        public void When_field_is_ignored_expect_it_ineligible()
        {
            var root = new VisualElement();
            var field = new TextField();
            root.Add(field);

            Assert.That(TextFieldFocus.IsEligible(field, root, root, ignored => ignored == field), Is.False);
        }

        [Test]
        public void When_field_is_disabled_expect_it_ineligible()
        {
            var root = new VisualElement();
            var field = new TextField();
            root.Add(field);
            field.SetEnabled(false);

            Assert.That(TextFieldFocus.IsEligible(field, root, root, null), Is.False,
                "A disabled field should not move the screen.");
        }

        [Test]
        public void When_field_is_inside_the_movable_element_expect_it_eligible()
        {
            var root = new VisualElement();
            var field = new TextField();
            root.Add(field);

            Assert.That(TextFieldFocus.IsEligible(field, root, root, null), Is.True);
        }
    }
}
#endif
