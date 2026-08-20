# Shared Components Sprint Plan

## Overview
This sprint plan outlines the refactoring of shared components into `Chat.Client.Shared` to maximize code reuse between polling and duplex clients. The goal is to reduce duplication, improve maintainability, and ensure consistent UX across both client implementations.

## Current State
- ✅ Shared styles (Colors.xaml, Sizing.xaml)
- ✅ Shared converters (FileSizeConverter)
- ✅ Shared resources (Converters.xaml)
- ❌ No shared services
- ❌ No shared models
- ❌ No shared controls
- ❌ No shared helpers

## Sprint Structure

### Sprint 1: Foundation Services (High Priority)
**Goal:** Establish core shared services that both clients can use immediately.

**Tasks:**
1. **Configuration Service**
   - Create `Services/ConfigurationService.cs`
   - Handle host/port configuration from App.config
   - Handle command-line argument parsing
   - Provide centralized settings access
   - Support settings persistence (optional)
   
2. **File Helper Service**
   - Create `Services/FileHelperService.cs`
   - File size validation (2MB limit)
   - File path validation
   - File download/upload helpers
   - Safe file operations

3. **Validation Service**
   - Create `Services/ValidationService.cs`
   - Username validation
   - Channel name validation
   - Message validation
   - Common validation rules

**Dependencies:** None
**Estimated Effort:** 2-3 hours
**Impact:** Immediate benefit - reduces duplication in both clients

---

### Sprint 2: Helper Utilities (High Priority)
**Goal:** Create reusable utility classes for common operations.

**Tasks:**
1. **Formatting Helpers**
   - Create `Helpers/FormattingHelper.cs`
   - Timestamp formatting (relative time, absolute time)
   - Message display formatting
   - File size formatting (move logic from converter)
   
2. **String Helpers**
   - Create `Helpers/StringHelper.cs`
   - Truncation helpers
   - Sanitization helpers
   - Common string operations

3. **DateTime Helpers**
   - Create `Helpers/DateTimeHelper.cs`
   - Time zone handling
   - Relative time calculations
   - Display formatting

**Dependencies:** None
**Estimated Effort:** 1-2 hours
**Impact:** Reduces code duplication, improves consistency

---

### Sprint 3: Shared Models (Medium Priority)
**Goal:** Define client-specific models that can be shared.

**Tasks:**
1. **UI State Models**
   - Create `Models/UiState.cs`
   - Application state (signed in/out, current channel)
   - Theme state
   - Settings state
   
2. **Display Models**
   - Create `Models/MessageDisplayModel.cs`
   - Create `Models/ChannelDisplayModel.cs`
   - Create `Models/UserDisplayModel.cs`
   - Wrap contract types with display-specific properties

3. **Settings Models**
   - Create `Models/AppSettings.cs`
   - Host, port, theme preferences
   - Polling interval settings
   - Validation rules

**Dependencies:** Sprint 1 (Configuration Service)
**Estimated Effort:** 2-3 hours
**Impact:** Enables better separation of concerns, prepares for MVVM

---

### Sprint 4: Shared Controls - Phase 1 (Medium Priority)
**Goal:** Create reusable UserControls for common UI elements.

**Tasks:**
1. **SignIn Control**
   - Create `Controls/SignInControl.xaml` + code-behind
   - Username input with validation
   - Sign-in button with loading state
   - Error message display
   - Events: SignInRequested
   
2. **Message Bubble Control**
   - Create `Controls/MessageBubbleControl.xaml` + code-behind
   - Display message with sender, timestamp, content
   - Different styles for own vs others' messages
   - File message display
   - Private message indicator

3. **File Item Control**
   - Create `Controls/FileItemControl.xaml` + code-behind
   - Display file name, size, uploader, timestamp
   - Download button
   - Click event for file operations

**Dependencies:** Sprint 2 (Formatting Helpers), Sprint 3 (Display Models)
**Estimated Effort:** 4-5 hours
**Impact:** Significant UI consistency, reduces XAML duplication

---

### Sprint 5: Shared Controls - Phase 2 (Medium Priority)
**Goal:** Create more complex UserControls for major UI sections.

**Tasks:**
1. **Channel List Control**
   - Create `Controls/ChannelListControl.xaml` + code-behind
   - Channel list display
   - Create/join channel UI
   - Member count display
   - Events: ChannelSelected, CreateChannel, JoinChannel
   
2. **Conversation Control**
   - Create `Controls/ConversationControl.xaml` + code-behind
   - Message list display
   - Input area
   - Send button
   - Member list sidebar
   - Shared files section
   - Events: SendMessage, LeaveChannel, FileShare

3. **Settings Control**
   - Create `Controls/SettingsControl.xaml` + code-behind
   - Host/port configuration
   - Theme toggle
   - Polling interval setting
   - Events: SettingsChanged

**Dependencies:** Sprint 4 (Phase 1 Controls)
**Estimated Effort:** 6-8 hours
**Impact:** Major UI consistency, enables rapid duplex client development

---

### Sprint 6: ViewModels (Low Priority)
**Goal:** Implement MVVM pattern with shared base classes.

**Tasks:**
1. **Base ViewModel**
   - Create `ViewModels/BaseViewModel.cs`
   - INotifyPropertyChanged implementation
   - Common commands
   - Validation support
   
2. **Specific ViewModels**
   - Create `ViewModels/SignInViewModel.cs`
   - Create `ViewModels/ChannelListViewModel.cs`
   - Create `ViewModels/ConversationViewModel.cs`
   - Create `ViewModels/SettingsViewModel.cs`

**Dependencies:** Sprint 3 (Models), Sprint 5 (Controls)
**Estimated Effort:** 4-6 hours
**Impact:** Better testability, separation of concerns (architectural improvement)

---

## Sprint Order Recommendation

1. **Sprint 1** (Foundation Services) - Start here for immediate value
2. **Sprint 2** (Helper Utilities) - Quick wins, low risk
3. **Sprint 3** (Shared Models) - Enables better architecture
4. **Sprint 4** (Shared Controls Phase 1) - Start UI sharing
5. **Sprint 5** (Shared Controls Phase 2) - Complete UI sharing
6. **Sprint 6** (ViewModels) - Architectural improvement (optional)

## Benefits by Sprint

| Sprint | Code Duplication Reduced | Consistency Improved | Maintainability |
|--------|------------------------|---------------------|-----------------|
| Sprint 1 | 15-20% | Low | High |
| Sprint 2 | 5-10% | Medium | Medium |
| Sprint 3 | 5-10% | Medium | High |
| Sprint 4 | 20-30% | High | High |
| Sprint 5 | 30-40% | Very High | Very High |
| Sprint 6 | 10-15% | High | Very High |

## Risk Assessment

**Low Risk:**
- Sprint 1 (Services) - Isolated, easy to test
- Sprint 2 (Helpers) - Pure functions, no side effects

**Medium Risk:**
- Sprint 3 (Models) - Requires refactoring existing code
- Sprint 4 (Controls Phase 1) - UI changes, need testing

**High Risk:**
- Sprint 5 (Controls Phase 2) - Major UI refactoring
- Sprint 6 (ViewModels) - Architectural change, significant effort

## Success Criteria

- All shared components have unit tests
- Polling client successfully refactored to use shared components
- Duplex client can be built using 80%+ shared components
- No regression in existing functionality
- Build time remains acceptable
- Code coverage maintained or improved

## Notes

- Each sprint should be completed and tested before moving to the next
- Consider creating integration tests for shared services
- Document public APIs for shared components
- Use semantic versioning for Chat.Client.Shared assembly
- Consider creating a sample/demo project for shared controls
