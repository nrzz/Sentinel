package sentinel

const (
	DefaultBaseURL       = "http://localhost:5000"
	DefaultTenantHeader  = "X-Tenant-ID"
	DefaultTimeout       = 30
	DefaultMaxRetryCount = 3
)

type LoginRequest struct {
	Email    string  `json:"email"`
	Password string  `json:"password"`
	TenantID *string `json:"tenantId,omitempty"`
}

type RefreshTokenRequest struct {
	RefreshToken string `json:"refreshToken"`
}

type AuthUser struct {
	ID          string   `json:"id"`
	Email       string   `json:"email"`
	DisplayName string   `json:"displayName"`
	TenantID    string   `json:"tenantId"`
	Roles       []string `json:"roles"`
	Permissions []string `json:"permissions"`
}

type AuthResponse struct {
	AccessToken          string    `json:"accessToken"`
	RefreshToken         string    `json:"refreshToken"`
	AccessTokenExpiresAt string    `json:"accessTokenExpiresAt"`
	User                 AuthUser  `json:"user"`
}

type LogEntryInput struct {
	Service       string            `json:"service"`
	Environment   string            `json:"environment"`
	Level         string            `json:"level"`
	Message       string            `json:"message"`
	Attributes    map[string]string `json:"attributes,omitempty"`
	TraceID       *string           `json:"traceId,omitempty"`
	SpanID        *string           `json:"spanId,omitempty"`
	CorrelationID *string           `json:"correlationId,omitempty"`
	Timestamp     *string           `json:"timestamp,omitempty"`
}

type IngestLogsRequest struct {
	Logs []LogEntryInput `json:"logs"`
}

type IngestLogsResponse struct {
	AcceptedCount int    `json:"acceptedCount"`
	Status        string `json:"status"`
}

type LogSearchQuery struct {
	From        *string
	To          *string
	Level       *string
	Service     *string
	Environment *string
	Query       *string
	TraceID     *string
	Limit       int
	Offset      int
}

type LogSearchItem struct {
	ID              string  `json:"id"`
	Timestamp       string  `json:"timestamp"`
	Service         string  `json:"service"`
	Environment     string  `json:"environment"`
	Level           string  `json:"level"`
	NormalizedLevel string  `json:"normalizedLevel"`
	Message         string  `json:"message"`
	TraceID         *string `json:"traceId,omitempty"`
	SpanID          *string `json:"spanId,omitempty"`
	CorrelationID   *string `json:"correlationId,omitempty"`
	ParsedException *string `json:"parsedException,omitempty"`
	SourceHost      *string `json:"sourceHost,omitempty"`
}

type SearchLogsResponse struct {
	Items      []LogSearchItem `json:"items"`
	TotalCount int64           `json:"totalCount"`
	Limit      int             `json:"limit"`
	Offset     int             `json:"offset"`
}

type MetricEntryInput struct {
	Name        string            `json:"name"`
	Value       float64           `json:"value"`
	Unit        string            `json:"unit"`
	Tags        map[string]string `json:"tags,omitempty"`
	Service     string            `json:"service"`
	Environment string            `json:"environment"`
	Timestamp   *string           `json:"timestamp,omitempty"`
}

type IngestMetricsRequest struct {
	Metrics []MetricEntryInput `json:"metrics"`
}

type IngestMetricsResponse struct {
	AcceptedCount int    `json:"acceptedCount"`
	Status        string `json:"status"`
}

type MetricQuery struct {
	From        *string
	To          *string
	Name        *string
	Service     *string
	Environment *string
	Limit       int
}

type MetricItem struct {
	ID          string            `json:"id"`
	Timestamp   string            `json:"timestamp"`
	Name        string            `json:"name"`
	Value       float64           `json:"value"`
	Unit        string            `json:"unit"`
	Service     string            `json:"service"`
	Environment string            `json:"environment"`
	Tags        map[string]string `json:"tags"`
}

type QueryMetricsResponse struct {
	Items []MetricItem `json:"items"`
}

type AlertRule struct {
	ID                        string   `json:"id"`
	TenantID                  string   `json:"tenantId"`
	Name                      string   `json:"name"`
	Description               string   `json:"description"`
	Query                     string   `json:"query"`
	Condition                 string   `json:"condition"`
	Severity                  string   `json:"severity"`
	Status                    string   `json:"status"`
	EvaluationIntervalSeconds int64    `json:"evaluationIntervalSeconds"`
	NotificationChannels      []string `json:"notificationChannels"`
	CreatedBy                 *string  `json:"createdBy,omitempty"`
	CreatedAt                 string   `json:"createdAt"`
	UpdatedAt                 string   `json:"updatedAt"`
}

type Incident struct {
	ID                      string  `json:"id"`
	Title                   string  `json:"title"`
	Description             string  `json:"description"`
	Severity                string  `json:"severity"`
	Status                  string  `json:"status"`
	AssignedTo              *string `json:"assignedTo,omitempty"`
	SourceAlertExecutionID  *string `json:"sourceAlertExecutionId,omitempty"`
	CreatedBy               *string `json:"createdBy,omitempty"`
	ResolvedAt              *string `json:"resolvedAt,omitempty"`
	CreatedAt               string  `json:"createdAt"`
	UpdatedAt               string  `json:"updatedAt"`
}

type Tenant struct {
	ID           string `json:"id"`
	Name         string `json:"name"`
	Slug         string `json:"slug"`
	IsActive     bool   `json:"isActive"`
	Environment  string `json:"environment"`
	SettingsJSON string `json:"settingsJson"`
	CreatedAt    string `json:"createdAt"`
	UpdatedAt    string `json:"updatedAt"`
}

type APIError struct {
	StatusCode   int
	Message      string
	ResponseBody string
}

func (e *APIError) Error() string {
	return "sentinel API error " + itoa(e.StatusCode) + ": " + e.Message
}

func itoa(value int) string {
	if value == 0 {
		return "0"
	}

	negative := value < 0
	if negative {
		value = -value
	}

	digits := make([]byte, 0, 12)
	for value > 0 {
		digits = append(digits, byte('0'+value%10))
		value /= 10
	}

	if negative {
		digits = append(digits, '-')
	}

	for left, right := 0, len(digits)-1; left < right; left, right = left+1, right-1 {
		digits[left], digits[right] = digits[right], digits[left]
	}

	return string(digits)
}
