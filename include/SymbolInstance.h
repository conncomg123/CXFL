#ifndef SYMBOLINSTANCE_H
#define SYMBOLINSTANCE_H

#include "Instance.h"
#include "Matrix.h"
#include "Point.h"
#include <optional>
#include <memory>

class SymbolInstance : public Instance {
private:
	Matrix matrix;
	Point point;
	bool selected;
	unsigned int firstFrame;
	std::optional<unsigned int> lastFrame;
	std::string symbolType;
	std::string loop;
	double getWidthRecur() const noexcept;
	double getHeightRecur() const noexcept;
public:
	SymbolInstance(pugi::xml_node& elementNode) noexcept;
	~SymbolInstance() noexcept override;
	SymbolInstance(SymbolInstance& symbolInstance) noexcept;
	bool isSelected() const noexcept;
	void setSelected(bool selected) noexcept;
	std::string getSymbolType() const noexcept;
	void setSymbolType(const std::string& symbolType) noexcept;
	unsigned int getFirstFrame() const noexcept;
	void setFirstFrame(unsigned int firstFrame) noexcept;
	std::optional<unsigned int> getLastFrame() const noexcept;
	void setLastFrame(unsigned int lastFrame) noexcept;
	void setLastFrame(std::optional<unsigned int> lastFrame) noexcept;
	std::string getLoop() const noexcept;
	void setLoop(const std::string& loop) noexcept;
	double getWidth() const noexcept override;
	double getHeight() const noexcept override;
	Matrix& getMatrix() noexcept;
	const Matrix& getMatrix() const noexcept;
	Point& getPoint() noexcept;
	const Point& getPoint() const noexcept;
};

#endif // SYMBOLINSTANCE_H