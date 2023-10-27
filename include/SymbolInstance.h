#ifndef SYMBOLINSTANCE_H
#define SYMBOLINSTANCE_H

#include "Instance.h"
#include "Matrix.h"
#include "Point.h"
#include <optional>
#include <memory>

class SymbolInstance : public Instance {
private:
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
	const std::string& getSymbolType() const noexcept;
	void setSymbolType(const std::string& symbolType) noexcept;
	unsigned int getFirstFrame() const noexcept;
	void setFirstFrame(unsigned int firstFrame) noexcept;
	std::optional<unsigned int> getLastFrame() const noexcept;
	void setLastFrame(unsigned int lastFrame) noexcept;
	void setLastFrame(std::optional<unsigned int> lastFrame) noexcept;
	const std::string& getLoop() const noexcept;
	void setLoop(const std::string& loop) noexcept;
	double getWidth() const noexcept override;
	double getHeight() const noexcept override;
};

#endif // SYMBOLINSTANCE_H